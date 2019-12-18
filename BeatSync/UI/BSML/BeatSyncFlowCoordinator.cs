using System;
using HMUI;
using BeatSaberMarkupLanguage;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using BeatSaberMarkupLanguage.ViewControllers;

namespace BeatSync.UI.BSML
{
    internal class BeatSyncFlowCoordinator : FlowCoordinator
    {
        ConfigUiBase _centerViewController;
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            try
            {
                if (firstActivation)
                {
                    Logger.log?.Warn("First activation");
                    _centerViewController = BeatSaberUI.CreateViewController<BeatSyncSettings>();
                    _centerViewController.name = "BSMLPractice.CenterView";
                    
                    Logger.log?.Warn("Center created");
                    _leftViewController = BeatSaberUI.CreateViewController<ExampleViewLeft>();
                    _leftViewController.name = "BSMLPractice.LeftView";
                    Logger.log?.Warn("Left created");
                    _rightViewController = BeatSaberUI.CreateViewController<ExampleViewRight>();
                    _rightViewController.name = "BSMLPractice.RightView";
                    Logger.log?.Warn("Right created");
                    base.title = "BSMLPractice";
                    showBackButton = true;
                    ProvideInitialViewControllers(_centerViewController, _leftViewController, _rightViewController);
                    
                }
                if (activationType == ActivationType.AddedToHierarchy)
                {
                    Logger.log?.Warn("AddedToHierarchy");
                    _leftViewController.OnBackPressed -= BackButtonWasPressed;
                    _leftViewController.OnBackPressed += BackButtonWasPressed;
                }
                else
                {
                    Logger.log?.Warn("NotAddedToHierarchy");
                }
            }
            catch (Exception ex)
            {
                Logger.log?.Error(ex);
            }
        }

        #region From BSIPA-ModList
        private delegate void PresentFlowCoordDel(FlowCoordinator self, FlowCoordinator newF, Action finished, bool immediate, bool replaceTop);
        private static PresentFlowCoordDel presentFlow;

        public void Present(Action finished = null, bool immediate = false, bool replaceTop = false)
        {
            if (presentFlow == null)
            {
                var ty = typeof(FlowCoordinator);
                var m = ty.GetMethod("PresentFlowCoordinator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                presentFlow = (PresentFlowCoordDel)Delegate.CreateDelegate(typeof(PresentFlowCoordDel), m);
            }

            MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
            presentFlow(mainFlow, this, finished, immediate, replaceTop);
        }

        private delegate void DismissFlowDel(FlowCoordinator self, FlowCoordinator newF, Action finished, bool immediate);
        private static DismissFlowDel dismissFlow;

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            Logger.log?.Info($"BackButtonWasPressed. topViewController is {topViewController?.name ?? "null"}");
            if (dismissFlow == null)
            {
                var dismissMethod = typeof(FlowCoordinator).GetMethod("DismissFlowCoordinator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                dismissFlow = (DismissFlowDel)Delegate.CreateDelegate(typeof(DismissFlowDel), dismissMethod);
            }
            MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
            dismissFlow(mainFlow, this, null, false);
        }
        #endregion
    }
}
