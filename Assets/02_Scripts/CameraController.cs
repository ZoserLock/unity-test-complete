using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Class that controls the movement of the camera in the application
// TODO: Check Limits
public class CameraController : MonoBehaviour
{
    public enum CameraDragState
    {
        Idle = 0,
        Begin,
        Move,
        End,
    };
    
    public enum CameraDragAction
    {
        None,
        Pan,
        Tilt
    };

    private Camera _camera = null;

    // The current state of any drag.
    private CameraDragState _dragState   = CameraDragState.Idle;

    // The current type of drag being donw. Can be Pan or Tilt
    private CameraDragAction _dragAction = CameraDragAction.None;

    // Start position of the drag
    private Vector3 _screenDragStartPosition;
    // The current position of the drag while we are moving the drag.
    private Vector3 _screenDragCurrentPosition;
    
    // The poing in space that started the drag. 
    private Vector3 _worldDragStartPosition;
    // The poing in far frustum plane when the drag started.
    private Vector3 _worldDragStartFarPosition;

    // The initial position of the camera when the drag started.
    private Vector3 _cameraDragStartPosition;

    // The current viewport position of the current drag. Used by tilt
    private Vector3 _cameraDragCurrentViewportPosition;

    public Camera Camera
    {
        get { return _camera; }
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();

        if(_camera == null)
        {
            Debug.LogError("CameraController: Camera component not found");
        }
    }

    private void OnEnable()
    {
        _dragAction = CameraDragAction.None;
        _dragState = CameraDragState.Idle;
    }

    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                if (_dragState == CameraDragState.Idle)
                {
                    _dragState = CameraDragState.Begin;
                    _dragAction = CameraDragAction.Tilt;
                }
            }
        }
        else
        {
            if (_dragAction == CameraDragAction.Tilt)
            {
                if (_dragState == CameraDragState.Move)
                {
                    _dragState = CameraDragState.End;
                }
            }
        }

        if (Input.GetMouseButton(2))
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                if (_dragState == CameraDragState.Idle)
                {
                    _dragState = CameraDragState.Begin;
                    _dragAction = CameraDragAction.Pan;
                }
            }

        }else
        {
            if (_dragAction == CameraDragAction.Pan)
            {
                if (_dragState == CameraDragState.Move)
                {
                    _dragState = CameraDragState.End;
                }
            }
        }

        // Process wheel input and camera zoom.
        if (_dragState == CameraDragState.Idle)
        {
            float wheelDelta = Input.mouseScrollDelta.y;
            if (wheelDelta > 0 || wheelDelta < 0)
            {
                // Zoom is done in the direction of the mouse.
                Ray pickRay = _camera.ScreenPointToRay(Input.mousePosition);

                var target = ProjectRayToGround(pickRay);

                var moveVector = (target - _camera.transform.position);

                if ((moveVector.magnitude > 5 && wheelDelta>0) || (moveVector.magnitude < 60 && wheelDelta < 0))
                {
                    _camera.transform.position += moveVector.normalized * wheelDelta * 2.0f;
                }
            }
        }

        // Update Camera Panning and Tilt handle
        if (_dragState == CameraDragState.Begin)
        {
            // Update screen positions
            _screenDragStartPosition   = Input.mousePosition;
            _screenDragCurrentPosition = Input.mousePosition;

            _cameraDragStartPosition = transform.position;

            if (_dragAction == CameraDragAction.Pan)
            {
                Ray pickRay = _camera.ScreenPointToRay(_screenDragStartPosition);

                _worldDragStartPosition    = ProjectRayToGround(pickRay);
                _worldDragStartFarPosition = ProjectRayToCameraFarPlane(pickRay);

            }
            else if (_dragAction == CameraDragAction.Tilt)
            {
                _worldDragStartPosition            = ProjectRayToGround(new Ray(_camera.transform.position,_camera.transform.forward));
                _cameraDragCurrentViewportPosition = _camera.ScreenToViewportPoint(_screenDragStartPosition);
            }
            _dragState = CameraDragState.Move;
        }

        if (_dragState == CameraDragState.Move)
        {
            _screenDragCurrentPosition = Input.mousePosition;

            if (_dragAction == CameraDragAction.Pan)
            {
                // This panning algorithm some people call it perfect panning as it maintains the cursor in the original position while panning.
                Ray pickRay = _camera.ScreenPointToRay(_screenDragCurrentPosition);

                 var worldDragCurrentFarPosition = ProjectRayToCameraFarPlane(pickRay);

                float moveDistance = (_cameraDragStartPosition - _worldDragStartPosition).magnitude
                                   * (_worldDragStartFarPosition - worldDragCurrentFarPosition).magnitude
                                   / (_worldDragStartFarPosition - _worldDragStartPosition).magnitude;

                var dir = (_worldDragStartFarPosition - worldDragCurrentFarPosition).normalized;

                _camera.transform.position = _cameraDragStartPosition + dir * moveDistance;
            }
            else if (_dragAction == CameraDragAction.Tilt)
            {
                // This is a camera center targeted Orbital Rotation.
                var lastViewportPosition = _cameraDragCurrentViewportPosition;

                _cameraDragCurrentViewportPosition = _camera.ScreenToViewportPoint(_screenDragCurrentPosition);

                var deltaViewportMovement = _cameraDragCurrentViewportPosition - lastViewportPosition;

                _camera.transform.RotateAround(_worldDragStartPosition, Vector3.up, deltaViewportMovement.x  * 60);
                _camera.transform.RotateAround(_worldDragStartPosition, _camera.transform.right, -deltaViewportMovement.y * 60);
            }
        }

        if(_dragState == CameraDragState.End)
        {
            _screenDragCurrentPosition = Input.mousePosition;

            _dragState  = CameraDragState.Idle;
            _dragAction = CameraDragAction.None;
        }

        // Down allow the camera go too far away
        if(_camera.transform.position.magnitude>80)
        {
            _camera.transform.position = Vector3.ClampMagnitude(_camera.transform.position, 80);
        }
    }

    public Vector3 ProjectRayToCameraFarPlane(Ray pickRay)
    {
        Plane farPlane = new Plane(-_camera.transform.forward, _camera.farClipPlane);

        float enter = 0.0f;

        if (farPlane.Raycast(pickRay, out enter))
        {
            return pickRay.GetPoint(enter);
        }

        return Vector3.zero;
    }

    public Vector3 ProjectRayToGround(Ray pickRay)
    {
        RaycastHit hit;

        if (Physics.Raycast(pickRay, out hit))
        {
            return hit.point;
        }

        return Vector3.zero;
    }

    public bool TrySelectShovelAtMousePosition(out ShovelVisual shovelVisual)
    {
        Ray pickRay = _camera.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;

        if (Physics.Raycast(pickRay, out hit))
        {
            shovelVisual = hit.transform.gameObject.GetComponent<ShovelVisual>();

            return shovelVisual != null;
        }

        shovelVisual = null; 
        return false;
    }
}
