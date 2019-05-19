﻿namespace RSToolkit.UI
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(RawImage))]
    public class UIPageBackground : MonoBehaviour
    {
        public Texture DefaultBackgroundTexture;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetBackground(UIPage page)
        {
            var pageBackground = this.GetComponent<RawImage>();
            if (page.BackgroundImage != null)
            {
                pageBackground.texture = page.BackgroundImage;
                pageBackground.SetNativeSize();
            }
            else
            {
                pageBackground.texture = DefaultBackgroundTexture;
            }

            pageBackground.color = page.BackgroundColor;
            pageBackground.SetNativeSize();
        }

        #region Page Events

        void onNavigatedTo(UIPage page, bool keepCache)
        {
            SetBackground(page);
        }

        #endregion Page Events
    }
}