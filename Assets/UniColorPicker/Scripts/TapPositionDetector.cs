using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Linq;


namespace UniColorPicker
{
    public class TapPositionDetector : MonoBehaviour
    {
        private const float TapMargin = 0.1f;

        [SerializeField] private RectTransform _tapTarget = null;
        [SerializeField] private Canvas _canvas = null;

        public SoundPlayer sound;

        bool selecting = false;
        
        private Camera _camera;
        private float _height;
        private float _width;
        private Action<Vector2> _onValueChanged;

        private GraphicRaycaster _raycaster;
        private EventSystem _eventSystem;

        public void SetOnValueChanged(Action<Vector2> onValueChanged)
        {
            _onValueChanged = onValueChanged;
        }

        void Awake()
        {
            _camera = _canvas.worldCamera;
            var sizeDelta = _tapTarget.sizeDelta;
            _height = sizeDelta.y;
            _width = sizeDelta.x;

            _raycaster = _canvas.GetComponent<GraphicRaycaster>();
            _eventSystem = EventSystem.current;
        }

        public void ReceiveCommand(string command)
        {
            string[] parts = Regex.Split(command, "G")
                .Select(part => Regex.Replace(part, @"[^a-zA-Z0-9\s]", "").ToLower()) // clean and lowercase
                .Where(part => !string.IsNullOrWhiteSpace(part)) // remove empty
                .ToArray();

            foreach (string word in parts)
            {
                if(word == "select")
                {
                    sound.playSelect();
                    selecting = !selecting;
                }
            }
        }

        void Update()
        {
            if (selecting)
            {
                // Simulate a pointer at the center of the screen
                PointerEventData pointerData = new PointerEventData(_eventSystem);
                pointerData.position = new Vector2(Screen.width / 2f, Screen.height / 2f);

                // Raycast from the center of the screen
                List<RaycastResult> results = new List<RaycastResult>();

                _raycaster.Raycast(pointerData, results);

                Debug.Log(results.Count);

                foreach (var result in results)
                {

                    if (result.gameObject == _tapTarget.gameObject)
                    {
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            _tapTarget, pointerData.position, _camera, out var localPoint);

                        var rect = _tapTarget.rect;
                        var pivot = _tapTarget.pivot;

                        float progressX = (localPoint.x / rect.width) + pivot.x;
                        float progressY = (localPoint.y / rect.height) + pivot.y;

                        if (progressX < -TapMargin || progressX > 1 + TapMargin || progressY < -TapMargin || progressY > 1 + TapMargin)
                            return;

                        progressX = Mathf.Clamp01(progressX);
                        progressY = Mathf.Clamp01(progressY);

                        _onValueChanged?.Invoke(new Vector3(progressX, progressY));
                        return;
                    }
                }
            }
        }

            
    }
}
