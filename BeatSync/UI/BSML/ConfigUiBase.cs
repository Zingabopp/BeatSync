using BeatSaberMarkupLanguage.Notify;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync.UI.BSML
{
    public abstract class ConfigUiBase : HotReloadableViewController, INotifiableHost
    {
        private static readonly string _viewDirectory = @"UserData\BeatSyncViews";
        protected string ViewDirectory => _viewDirectory;

    }
}
