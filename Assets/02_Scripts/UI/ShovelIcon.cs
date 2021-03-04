using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShovelIcon : MonoBehaviour
{
    [SerializeField]
    private Text _text = null;

    private Camera _camera = null;
    private Transform _attachPoint = null;

    public void SetAttachPoint(Transform attachPoint)
    {
        _attachPoint = attachPoint;
    }

    public void SetCamera(Camera camera)
    {
        _camera = camera;
    }

    public void SetText(string text)
    {
        _text.text = text;
    }

    void Update()
    {
        if(_camera != null && _attachPoint != null)
        {
            var screenPoint = _camera.WorldToScreenPoint(_attachPoint.position);

            transform.position = screenPoint;
        }
    }
}
