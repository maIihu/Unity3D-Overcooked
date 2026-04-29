using Counter;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class ObjectPlacementController : MonoBehaviour
{
    public event Action<BaseCounter> OnCounterSelected;
    public event Action OnCounterDeselected;

    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float snapValue = 1f;
    
    private GameObject _currentSelection;
    private bool _isDragging;
    private Vector3 _dragOffset;

    private void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleSelection();
        }

        if (Input.GetMouseButton(0) && _isDragging && _currentSelection != null)
        {
            HandleMove();
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
        }

        if (_currentSelection != null)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                _currentSelection.transform.Rotate(0, 90, 0);
            }

            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
            {
                 DeleteCurrentSelection();
            }
        }
    }

    public void DeleteCurrentSelection()
    {
        if (_currentSelection == null) return;

        if (LevelDesignerManager.Instance != null)
        {
            LevelDesignerManager.Instance.RemoveCounter(_currentSelection.GetComponent<BaseCounter>());
        }
        Destroy(_currentSelection);
        _currentSelection = null;
        OnCounterDeselected?.Invoke();
    }

    private void HandleSelection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log($"Raycast hit: {hit.collider.name} on layer {hit.collider.gameObject.layer}");
            
            BaseCounter counter = hit.collider.GetComponentInParent<BaseCounter>();
            if (counter != null)
            {
                Debug.Log($"Selected counter: {counter.name}");
                _currentSelection = counter.gameObject;
                _isDragging = true;
                _dragOffset = _currentSelection.transform.position - GetMousePosOnPlane();
                OnCounterSelected?.Invoke(counter);
            }
            else
            {
                Debug.Log("Hit something, but it's not a counter.");
                if (_currentSelection != null) OnCounterDeselected?.Invoke();
                _currentSelection = null;
            }
        }
        else
        {
            Debug.Log("Raycast hit nothing.");
            if (_currentSelection != null) OnCounterDeselected?.Invoke();
            _currentSelection = null;
        }
    }

    private void HandleMove()
    {
        Vector3 mousePos = GetMousePosOnPlane();
        Vector3 newPos = mousePos + _dragOffset;
        
        newPos.x = Mathf.Round(newPos.x / snapValue) * snapValue;
        newPos.z = Mathf.Round(newPos.z / snapValue) * snapValue;
        newPos.y = 0;

        _currentSelection.transform.position = newPos;
    }

    private Vector3 GetMousePosOnPlane()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }
}
