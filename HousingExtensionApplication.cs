// <copyright file="HousingExtensionApplication.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System.Drawing;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using Jpp.Ironstone.Core;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Jpp.Ironstone.Core.UI;
using Jpp.Ironstone.Housing;
using Jpp.Ironstone.Housing.Commands;
using Jpp.Ironstone.Housing.Properties;
using Unity;

[assembly: ExtensionApplication(typeof(HousingExtensionApplication))]

namespace Jpp.Ironstone.Housing
{
    /// <summary>
    /// Application extension for housing module.
    /// </summary>
    public class HousingExtensionApplication : IIronstoneExtensionApplication
    {
        /// <summary>
        /// Gets current instance of housing module.
        /// </summary>
        public static HousingExtensionApplication Current { get; private set; }

        /// <summary>
        /// Gets or sets logger instances.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <inheritdoc/>
        public void CreateUI()
        {
            var cmdBlockFromPointAtGradient = UIHelper.GetCommandGlobalName(typeof(LevelBlockCommands), nameof(LevelBlockCommands.CalculateLevelFromPointAtGradient));
            var cmdBlockFromBlockAtGradient = UIHelper.GetCommandGlobalName(typeof(LevelBlockCommands), nameof(LevelBlockCommands.CalculateLevelFromLevelBlockAtGradient));
            var cmdBlockFromBlockWithInvert = UIHelper.GetCommandGlobalName(typeof(LevelBlockCommands), nameof(LevelBlockCommands.CalculateLevelFromLevelBlockWithInvert));
            var cmdBlockBetweenBlocks = UIHelper.GetCommandGlobalName(typeof(LevelBlockCommands), nameof(LevelBlockCommands.CalculateLevelBetweenLevels));
            var cmdGradientBetweenBlocks = UIHelper.GetCommandGlobalName(typeof(GradientBlockCommands), nameof(GradientBlockCommands.CalculateGradientBetweenLevels));
            var cmdPolylineFromLevelBlocks = UIHelper.GetCommandGlobalName(typeof(PolylineCommands), nameof(PolylineCommands.GeneratePolyline3dFromLevels));

            var btnBlockFromPointAtGradient = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnLevelBlockFromPointAtGradient, Resources.level_block_small, RibbonItemSize.Standard, cmdBlockFromPointAtGradient);
            var btnBlockFromBlockAtGradient = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnLevelBlockFromBlockAtGradient, Resources.level_block_small, RibbonItemSize.Standard, cmdBlockFromBlockAtGradient);
            var btnBlockBetweenBlocks = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnBlockBetweenBlocks, Resources.level_block_small, RibbonItemSize.Standard, cmdBlockBetweenBlocks);
            var btnBlockFromBlockWithInvert = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnLevelBlockFromBlockWithInvert, Resources.level_block_small, RibbonItemSize.Standard, cmdBlockFromBlockWithInvert);

            var btnGradientBetweenBlocks = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnGradientBetweenBlocks, Resources.gradient_large, RibbonItemSize.Standard, cmdGradientBetweenBlocks);
            var btnPolylineFromLevelBlocks = UIHelper.CreateButton(Resources.ExtensionApplication_UI_BtnPolylineFromLevelBlocks, Resources.line_from_levels_small, RibbonItemSize.Standard, cmdPolylineFromLevelBlocks);

            var btnSplitLevel = new RibbonSplitButton
            {
                ShowText = true,
                IsSynchronizedWithCurrentItem = false,
                Text = Resources.ExtensionApplication_UI_BtnLevelBlocks,
                Image = UIHelper.LoadImage(new Bitmap(Resources.level_block_large, new Size(16, 16))),
                IsSplit = false,
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
            column.Items.Add(new RibbonRowBreak());
            column.Items.Add(btnPolylineFromLevelBlocks);

            source.Items.Add(column);

            var panel = new RibbonPanel { Source = source };
            var ribbon = ComponentManager.Ribbon;
            var tab = ribbon.FindTab(Constants.IRONSTONE_CONCEPT_TAB_ID);
            tab.Panels.Add(panel);
        }

        /// <inheritdoc/>
        public void Initialize()
        {
            Current = this;
            CoreExtensionApplication._current.RegisterExtension(this);
        }

        /// <inheritdoc/>
        public void InjectContainer(IUnityContainer container)
        {
            this.Logger = container.Resolve<ILogger>();
        }

        /// <inheritdoc/>
        public void Terminate()
        {
        }
    }
}
