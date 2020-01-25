using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using Jpp.Ironstone.Core;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Jpp.Ironstone.Core.UI;
using Jpp.Ironstone.Housing;
using Jpp.Ironstone.Housing.Commands;
using Jpp.Ironstone.Housing.Properties;
using System.Drawing;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.EditorInput;
using Jpp.Ironstone.Housing.ObjectModel;
using Jpp.Ironstone.Housing.ObjectModel.Concept;
using NLog;
using Unity;
using Constants = Jpp.Ironstone.Core.Constants;
using ILogger = Jpp.Ironstone.Core.ServiceInterfaces.ILogger;

[assembly: ExtensionApplication(typeof(HousingExtensionApplication))]
namespace Jpp.Ironstone.Housing
{
    public class HousingExtensionApplication : IIronstoneExtensionApplication
    {
        public ILogger Logger { get; set; }
        public static HousingExtensionApplication Current { get; private set; }

        public void CreateUI()
        {
            // TODO: Adjust helper to include these, this is aweful
            var cmdBlockFromPointAtGradient = UIHelper.GetCommandGlobalName(typeof(LevelBlockCommands), nameof(LevelBlockCommands.CalculateLevelFromPointAtGradient));
            var cmdBlockFromBlockAtGradient = UIHelper.GetCommandGlobalName(typeof(LevelBlockCommands), nameof(LevelBlockCommands.CalculateLevelFromLevelBlockAtGradient));
            var cmdBlockFromBlockWithInvert = UIHelper.GetCommandGlobalName(typeof(LevelBlockCommands), nameof(LevelBlockCommands.CalculateLevelFromLevelBlockWithInvert));
            var cmdBlockBetweenBlocks = UIHelper.GetCommandGlobalName(typeof(LevelBlockCommands), nameof(LevelBlockCommands.CalculateLevelBetweenLevels));
            var cmdGradientBetweenBlocks = UIHelper.GetCommandGlobalName(typeof(GradientBlockCommands), nameof(GradientBlockCommands.CalculateGradientBetweenLevels));
            var cmdConceptualPlotCreate = UIHelper.GetCommandGlobalName(typeof(ConceptualPlotCommands), nameof(ConceptualPlotCommands.CreatePlot));

            var btnBlockFromPointAtGradient = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnLevelBlockFromPointAtGradient, Resources.level_block_small, RibbonItemSize.Standard, cmdBlockFromPointAtGradient);
            var btnBlockFromBlockAtGradient = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnLevelBlockFromBlockAtGradient, Resources.level_block_small, RibbonItemSize.Standard, cmdBlockFromBlockAtGradient);
            var btnBlockBetweenBlocks = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnBlockBetweenBlocks, Resources.level_block_small, RibbonItemSize.Standard, cmdBlockBetweenBlocks);
            var btnBlockFromBlockWithInvert = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnLevelBlockFromBlockWithInvert, Resources.level_block_small, RibbonItemSize.Standard, cmdBlockFromBlockWithInvert);
            var btnConceptualPlotCreate = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnConceptPlotCreate, Resources.concept_plot_small, RibbonItemSize.Standard, cmdConceptualPlotCreate);

            var btnGradientBetweenBlocks = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnGradientBetweenBlocks, Resources.gradient_large, RibbonItemSize.Standard, cmdGradientBetweenBlocks);

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
            var column = new RibbonRowPanel { IsTopJustified = true };

            column.Items.Add(btnSplitLevel);
            column.Items.Add(new RibbonRowBreak());
            column.Items.Add(btnGradientBetweenBlocks);

            var column2 = new RibbonRowPanel { IsTopJustified = true };
            column2.Items.Add(btnConceptualPlotCreate);

            source.Items.Add(column);
            source.Items.Add(column2);

            var panel = new RibbonPanel { Source = source };
            var ribbon = ComponentManager.Ribbon;
            var tab = ribbon.FindTab(Constants.IRONSTONE_CONCEPT_TAB_ID);
            tab.Panels.Add(panel);

            RibbonTab housingConceptTab = new RibbonTab();
            housingConceptTab.Title = Resources.ExtensionApplication_UI_HousingContextTabTitle;

            CoreUIExtensionApplication.Current.RegisterConceptTab(housingConceptTab, () =>
            {
                string activeName = Application.DocumentManager.MdiActiveDocument.Name;
                ConceptualPlotManager manager = DataService.Current.GetStore<HousingDocumentStore>(activeName).GetManager<ConceptualPlotManager>();
                return ContextualTabHelper.SelectionRestrictedToCollection(manager.ManagedObjects);
            });
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
