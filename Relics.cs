using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Timers;
using ExileCore2;
using ExileCore2.PoEMemory;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.Elements;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Enums;
using Vector2 = System.Numerics.Vector2;

namespace Relics;

public class Relics : BaseSettingsPlugin<RelicsSettings>
{
    private class RelicFrame
    {
        public ExileCore2.Shared.RectangleF Rectangle { get; set; }
        public Color Color { get; set; }
        public int Thickness { get; set; }
    }

    private const int UpdateInterval = 200;
    private List<RelicFrame> frameBuffer = new();
    private Stopwatch updateTimer;
    private bool updateRequired = false;

    public override bool Initialise()
    {
        //Perform one-time initialization here

        //Maybe load you custom config (only do so if builtin settings are inadequate for the job)
        //var configPath = Path.Join(ConfigDirectory, "custom_config.txt");
        //if (File.Exists(configPath))
        //{
        //    var data = File.ReadAllText(configPath);
        //}


        updateTimer = new Stopwatch();
        updateTimer.Start();

        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
        //Perform once-per-zone processing here
        //For example, Radar builds the zone map texture here
    }

    public override void Tick()
    {
        //Perform non-render-related work here, e.g. position calculation.
        //var a = Math.Sqrt(7);
        HandleTickEvent();
    }

    public override void Render()
    {
        //Any Imgui or Graphics calls go here. This is called after Tick
        // Graphics.DrawText($"Plugin {GetType().Name} is working.", new Vector2(100, 100), Color.Red);
        if (!IsSomethingOpen()) return;

        foreach (var frame in frameBuffer)
        {
            Graphics.DrawFrame(frame.Rectangle, frame.Color, frame.Thickness);
        }
    }

    public override void EntityAdded(Entity entity)
    {
        //If you have a reason to process every entity only once,
        //this is a good place to do so.
        //You may want to use a queue and run the actual
        //processing (if any) inside the Tick method.
    }

#nullable enable
    public void HandleTickEvent()
    {
        if (updateTimer.ElapsedMilliseconds < UpdateInterval)
            return;

        updateTimer.Restart();
        frameBuffer.Clear();

        if (IsSomethingOpen())
        {
            IList<NormalInventoryItem> items = [];
            if (IsStashOpen())
            {
                items = GetItemsFromStash();
            }
            else if (IsRelicLockerOpen())
            {
                items = GetItemsFromRelicLocker();
            }

            foreach (var item in items)
            {
                if (!ValidateItem(item))
                    continue;

                if (!RenderableRelic(item))
                    continue;

                frameBuffer.Add(BuildItemFrame(item, Color.Red, 2));
            }
        }
    }

    private bool IsSomethingOpen()
    {
        return IsStashOpen() || IsRelicLockerOpen();
    }

    private bool IsStashOpen() =>
        GameController?.Game?.IngameState?.IngameUi?.StashElement?.IsVisible == true;

    private bool IsRelicLockerOpen()
    {
        Element? leftPanel = GameController?.Game?.IngameState?.IngameUi?.OpenLeftPanel;
        bool isChildRelicLocker = leftPanel?.Children?.Any(x => x?.Text?.Contains("Relic Locker") == true && x?.IsVisible == true) == true;
        LogMessage($"Relic Locker Visible: {isChildRelicLocker}");
        return leftPanel?.IsVisible == true && isChildRelicLocker;
    }

    private IList<NormalInventoryItem> GetItemsFromStash()
    {
        IList<NormalInventoryItem> items = [];
        StashElement? stash = GameController?.Game?.IngameState?.IngameUi?.StashElement;
        if (stash != null && stash.VisibleStash != null)
            items = stash.VisibleStash.VisibleInventoryItems;
        return items;
    }

    private Element? FindLockerElement()
    {
        Element? leftPanel = GameController?.Game?.IngameState?.IngameUi?.OpenLeftPanel;
        Element? locker = leftPanel?.Children?.FirstOrDefault(x => x?.Height > 100)? // first locker container in LeftPanel
            .Children?.FirstOrDefault(x => x?.Height > 100)? // second locker container in LeftPanel
            .Children?.FirstOrDefault(x => x?.Height > 100); // final locker container with items in children
        return locker;
    }

    private IList<NormalInventoryItem> GetItemsFromRelicLocker()
    {
        IList<NormalInventoryItem> items = [];
        Element relicLockerInventory = FindLockerElement();
        if (relicLockerInventory == null)
            return items;

        LogMessage($"RelicLockerInventory.Address: {relicLockerInventory?.Address}");
        LogMessage($"RelicLockerInventory.IsVisible: {relicLockerInventory?.IsVisible}");
        LogMessage($"RelicLockerInventory.ChildCount: {relicLockerInventory.ChildCount}");

        foreach (Element child in relicLockerInventory.Children)
        {
            if (child.Entity == null)
            {
                LogMessage($"Child Entity is null: {child.Address}");
                continue;
            }

            NormalInventoryItem item = GameController.Game.IngameState.GetObject<NormalInventoryItem>(child.Address);

            if (item == null)
            {
                LogMessage($"Child is not NormalInventoryItem: {child.Address}");
                continue;
            }

            if (item.Entity.Type != EntityType.Item)
            {
                LogMessage($"Child is not an item: {item.Entity.Type}");
                continue;
            }

            if (!item.Entity.Path.Contains("Relic"))
            {
                LogMessage($"Child is not a relic: {item.Entity.Path}");
                continue;
            }

            items.Add(item);
        }

        LogMessage($"Relic Locker Items: {items.Count}");

        return items;
    }

    private static bool ValidateItem(NormalInventoryItem item)
    {
        return item?.Entity?.Type == EntityType.Item && item?.Entity?.Path?.Contains("Relic") == true;
    }

    private static RelicFrame BuildItemFrame(NormalInventoryItem item, Color color, int thickness)
    {
        ExileCore2.Shared.RectangleF r = item.GetClientRect();
        return new RelicFrame
        {
            Rectangle = new ExileCore2.Shared.RectangleF(
                r.X + 3,
                r.Y + 3,
                r.Width - 6,
                r.Height - 6
            ),
            Color = color,
            Thickness = thickness,
        };
    }

    private bool RenderableRelic(NormalInventoryItem item)
    {
        return true;
    }
}