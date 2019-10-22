using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using Jpp.Ironstone.Core;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Jpp.Ironstone.Core.UI;
using Jpp.Ironstone.Housing;
using Jpp.Ironstone.Housing.Commands;
using Jpp.Ironstone.Housing.Properties;
using System.Windows.Controls;
using Unity;

[assembly: ExtensionApplication(typeof(HousingExtensionApplication))]
namespace Jpp.Ironstone.Housing
{
    public class HousingExtensionApplication : IIronstoneExtensionApplication
    {
        public ILogger Logger { get; set; }
        public static HousingExtensionApplication Current { get; private set; }

        public void CreateUI()
        {
            var cmdBlockFromPointAtGradient = UIHelper.GetCommandGlobalName(typeof(LevelBlockCommands), nameof(LevelBlockCommands.CalculateLevelFromPointAtGradient));
            var cmdBlockFromBlockAtGradient = UIHelper.GetCommandGlobalName(typeof(LevelBlockCommands), nameof(LevelBlockCommands.CalculateLevelFromLevelBlockAtGradient));
            var cmdBlockFromBlockWithInvert = UIHelper.GetCommandGlobalName(typeof(LevelBlockCommands), nameof(LevelBlockCommands.CalculateLevelFromLevelBlockWithInvert));
            var cmdGradientBetweenBlocks = UIHelper.GetCommandGlobalName(typeof(GradientBlockCommands), nameof(GradientBlockCommands.CalculateGradientBetweenLevels));

            var btnBlockFromPointAtGradient = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnLevelBlockFromPointAtGradient, Resources.level_block_small, RibbonItemSize.Standard, cmdBlockFromPointAtGradient);
            var btnBlockFromBlockAtGradient = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnLevelBlockFromBlockAtGradient, Resources.level_block_small, RibbonItemSize.Standard, cmdBlockFromBlockAtGradient);
            var btnBlockFromBlockWithInvert = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnLevelBlockFromBlockWithInvert, Resources.level_block_small, RibbonItemSize.Standard, cmdBlockFromBlockWithInvert);

            var btnGradientBetweenBlocks = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnGradientBetweenBlocks, Resources.gradient_large, RibbonItemSize.Large, cmdGradientBetweenBlocks);

            var btnSplitLevel = new RibbonSplitButton
            {
                ShowText = true,
                IsSplit = false,
                Size = RibbonItemSize.Large,
                LargeImage = UIHelper.LoadImage(Resources.level_block_large),
                Text = Resources.ExtensionApplication_UI_BtnLevelBlocks,
                Orientation = Orientation.Vertical,
                IsSynchronizedWithCurrentItem = false
            };

            btnSplitLevel.Items.Add(btnBlockFromPointAtGradient);
            btnSplitLevel.Items.Add(btnBlockFromBlockAtGradient);
            btnSplitLevel.Items.Add(btnBlockFromBlockWithInvert);

            var source = new RibbonPanelSource { Title = Resources.ExtensionApplication_UI_PanelTitle };
            source.Items.Add(btnSplitLevel);
            source.Items.Add(btnGradientBetweenBlocks);

            var panel = new RibbonPanel { Source = source };
            var ribbon = ComponentManager.Ribbon;
            var tab = ribbon.FindTab(Constants.IRONSTONE_CONCEPT_TAB_ID);
            tab.Panels.Add(panel);
        }

        public void Initialize()
        {
            Current = this;
            CoreExtensionApplication._current.RegisterExtension(this);
        }

        public void InjectContainer(IUnityContainer container)
        {
            Logger = container.Resolve<ILogger>();
        }

        public void Terminate() { }
    }
}
