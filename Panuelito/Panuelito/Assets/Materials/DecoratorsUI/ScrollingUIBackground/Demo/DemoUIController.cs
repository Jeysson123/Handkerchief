using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ShigeDev.ScrollingUIBackground;

namespace ShigeDev.ScrollingUIBackground {

    public class DemoUIController : MonoBehaviour
    {
        [SerializeField] private ScrollingUIBackground _scrollingUIBackground;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Sprite backgroundSprite1;
        [SerializeField] private Sprite backgroundSprite2;
        [SerializeField] private Sprite backgroundSprite3;


        public void ShowScrollingBackground1()
        {
            _scrollingUIBackground.StopScrolling();
            _backgroundImage.sprite = backgroundSprite1;
            _scrollingUIBackground.VerticalScroll = VerticalScroll.Up;
            _scrollingUIBackground.HorizontalScroll = HorizontalScroll.Left;
            _scrollingUIBackground.StartScrolling();
        }

        public void ShowScrollingBackground2()
        {
            _scrollingUIBackground.StopScrolling();
            _backgroundImage.sprite = backgroundSprite2;
            _scrollingUIBackground.VerticalScroll = VerticalScroll.Down;
            _scrollingUIBackground.HorizontalScroll = HorizontalScroll.None;
            _scrollingUIBackground.StartScrolling();
        }

        public void ShowScrollingBackground3()
        {
            _scrollingUIBackground.StopScrolling();
            _backgroundImage.sprite = backgroundSprite3;
            _scrollingUIBackground.VerticalScroll = VerticalScroll.None;
            _scrollingUIBackground.HorizontalScroll = HorizontalScroll.Right;
            _scrollingUIBackground.StartScrolling();
        }

    }
}
