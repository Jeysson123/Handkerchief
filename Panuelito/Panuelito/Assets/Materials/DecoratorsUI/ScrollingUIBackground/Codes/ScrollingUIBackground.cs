using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShigeDev.ScrollingUIBackground {

    public class ScrollingUIBackground : MonoBehaviour
    {
        [SerializeField, Tooltip("The speed of movement of the background.")]
        private float _scrollSpeed = 0.1f;
        [SerializeField, Tooltip("The vertical direction of the scroll.")]
        private VerticalScroll _verticalScroll;
        [SerializeField, Tooltip("The horizontal direction of the scroll.")]
        private HorizontalScroll _horizontalScroll;
        [SerializeField, Tooltip("Boolean variable which defines whether the background will animate the scrolling effect when the game starts.")]
        private bool _scrollToStart = true;
        [SerializeField, Tooltip("Boolean variable that defines whether the scrolling effect can be affected by the timescale. Note: since the pause system is often developed with a timescale.")]
        private bool _affectsTimeScale = true;
        [SerializeField, Tooltip("Reference of the image component which is the background.")]
        private Image _scrollableImage = null;
                
        private Canvas _canvas = null;
        private Material _material = null;
        private float _verticalScrollDirection = 0f;
        private float _horizontalScrollDirection = 0f;
        private float _scrollInterval = 0.02f; // Intervalo de tiempo entre desplazamientos
        private IEnumerator _scrollCoroutine;

        public float ScrollSpeed { get => _scrollSpeed; set => _scrollSpeed = value; }
        public VerticalScroll VerticalScroll { get => _verticalScroll; set => _verticalScroll = value; }
        public HorizontalScroll HorizontalScroll { get => _horizontalScroll; set => _horizontalScroll = value; }
        public bool ScrollToStart { get => _scrollToStart; set => _scrollToStart = value; }
        public bool AffectsTimeScale { get => _affectsTimeScale; set => _affectsTimeScale = value; }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            
            DefineScrollDirection();
        }

        private void Start()
        {
            _material = _scrollableImage.material;
            _material.mainTextureOffset =  Vector2.zero;

            if(_scrollToStart)
                StartScrolling();
        }

        /// <summary>Method to get the name of the character with its specifications as Rich Text.</summary>
        /// <param name="dialogueContent">DialogueContent that has the specifications to set for the character's name.</param>
        /// <returns>The name of the character plus its specifications as Rich Text</returns>

        /// <summary>Method which defines the vertical and horizontal scrolling direction.</summary>
        private void DefineScrollDirection()
        {
            DefineVerticalScrollDirection();
            DefineHorizontalScrollDirection();
        }

        /// <summary>Method which defines the vertical scrolling direction.</summary>
        private void DefineVerticalScrollDirection()
        {
            if(_verticalScroll == VerticalScroll.Up)
            {
                _verticalScrollDirection = -1f;
                return;
            }

            if(_verticalScroll == VerticalScroll.Down)
            {
                _verticalScrollDirection = 1f;
                return;
            }

            if(_verticalScroll == VerticalScroll.None)
            {
                _verticalScrollDirection = 0f;
                return;
            }
        }

        /// <summary>Method which defines the horizontal scrolling direction.</summary>
        private void DefineHorizontalScrollDirection() {
            if(_horizontalScroll == HorizontalScroll.Right)
            {
                _horizontalScrollDirection = -1f;
                return;
            }

            if(_horizontalScroll == HorizontalScroll.Left)
            {
                _horizontalScrollDirection = 1f;
                return;
            }

            if(_horizontalScroll == HorizontalScroll.None)
            {
                _horizontalScrollDirection = 0f;
                return;
            }
        }

        /// <summary>Coroutine which moves the texture of the background material.</summary>
        private IEnumerator ScrollTexture()
        {
            while (true)
            {
                _material.mainTextureOffset += new Vector2(
                    _horizontalScrollDirection * _scrollSpeed * _scrollInterval,
                    _verticalScrollDirection * _scrollSpeed * _scrollInterval
                );

                if(_affectsTimeScale)
                {
                    yield return new WaitForSeconds(_scrollInterval);
                }
                else
                {
                    yield return new WaitForSecondsRealtime(_scrollInterval);
                }
            }
        }

        /// <summary>Method which executes the Coroutine which moves the texture of the background material</summary>
        public void StartScrolling()
        {
            if(_scrollCoroutine != null)
                StopCoroutine(_scrollCoroutine);

            DefineScrollDirection();

            _scrollCoroutine = ScrollTexture();
            StartCoroutine(_scrollCoroutine);
        }

        /// <summary>Method which stop the Coroutine which moves the texture of the background material</summary>
        public void StopScrolling()
        {
            StopCoroutine(_scrollCoroutine);
        }

    }

    public enum VerticalScroll {
        Up,
        Down,
        None
    }

    public enum HorizontalScroll {
        Left,
        Right,
        None
    }

}