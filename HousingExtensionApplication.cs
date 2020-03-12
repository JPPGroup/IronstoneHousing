using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using Jpp.Ironstone.Core;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Jpp.Ironstone.Core.UI;
using Jpp.Ironstone.Housing;
using Jpp.Ironstone.Housing.Commands;
using Jpp.Ironstone.Housing.Properties;
using System.Drawing;
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
            var cmdBlockBetweenBlocks = UIHelper.GetCommandGlobalName(typeof(LevelBlockCommands), nameof(LevelBlockCommands.CalculateLevelBetweenLevels));
            var cmdGradientBetweenBlocks = UIHelper.GetCommandGlobalName(typeof(GradientBlockCommands), nameof(GradientBlockCommands.CalculateGradientBetweenLevels));
            var cmdPolylineFromLevelBlocks = UIHelper.GetCommandGlobalName(typeof(PolylineCommands), nameof(PolylineCommands.GeneratePolyline3dFromLevels));
            var cmdPlotPrelim = UIHelper.GetCommandGlobalName(typeof(PrelimPlotCommands), nameof(PrelimPlotCommands.PrelimPlotDetails));

            var btnBlockFromPointAtGradient = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnLevelBlockFromPointAtGradient, Resources.level_block_small, RibbonItemSize.Standard, cmdBlockFromPointAtGradient);
            var btnBlockFromBlockAtGradient = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnLevelBlockFromBlockAtGradient, Resources.level_block_small, RibbonItemSize.Standard, cmdBlockFromBlockAtGradient);
            var btnBlockBetweenBlocks = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnBlockBetweenBlocks, Resources.level_block_small, RibbonItemSize.Standard, cmdBlockBetweenBlocks);
            var btnBlockFromBlockWithInvert = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnLevelBlockFromBlockWithInvert, Resources.level_block_small, RibbonItemSize.Standard, cmdBlockFromBlockWithInvert);

            var btnGradientBetweenBlocks = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnGradientBetweenBlocks, Resources.gradient_large, RibbonItemSize.Standard, cmdGradientBetweenBlocks);
            var btnPolylineFromLevelBlocks = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnPolylineFromLevelBlocks, Resources.line_from_levels_small, RibbonItemSize.Standard, cmdPolylineFromLevelBlocks);

            var btnPlotPrelims = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnPlotPrelim, Resources.plot_small, RibbonItemSize.Standard, cmdPlotPrelim);

            var btnSplitLevel = new RibbonSplitButton
            {
                ShowText = true,
                IsSynchronizedWithCurrentItem = false,
                Text = Resources.ExtensionApplication_UI_BtnLevelBlocks,
                Image = UIHelper.LoadImage(new Bitmap(Resources.level_block_large, new Size(16, 16))),
                IsSplit = false
            };

            btnSplitLevel.Items.Add(btnBlockFromPointAtGradient);
            btnSplitLevel.Items.Add(btnBlockFromBlockAtGradient);
            btnSplitLevel.Items.Add(btnBlockFromBlockWithInvert);
            btnSplitLevel.Items.Add(btnBlockBetweenBlocks);

            var source = new RibbonPanelSource { Title = Resources.ExtensionApplication_UI_PanelTitle };
            var column1 = new RibbonRowPanel { IsTopJustified = true };
            var column2 = new RibbonRowPanel { IsTopJustified = true };

            column1.Items.Add(btnSplitLevel);
            column1.Items.Add(new RibbonRowBreak());
            column1.Items.Add(btnGradientBetweenBlocks);
            column1.Items.Add(new RibbonRowBreak());
            column1.Items.Add(btnPolylineFromLevelBlocks);
            
            column2.Items.Add(btnPlotPrelims);

            source.Items.Add(column1);
            source.Items.Add(column2);

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
