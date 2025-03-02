using System.Linq;
using Godot;
using Godot.Collections;

namespace HexMap.Scripts;

[GlobalClass]
public partial class HexMapEditor : Node3D
{
    [Export]
    public Array<Color> Colors { get; set; }

    [Export]
    public HexGrid HexGrid { get; set; }

    [Export]
    public Container CheckBoxContainer { get; set; }

    [Export]
    public Slider ElevationSlider { get; set; }

    [Export]
    public Label SliderTextLabel { get; set; }

    private Color CurrentColor { get; set; }
    public int CurrentElevation { get; set; }

    public override void _Ready()
    {
        CurrentColor = Colors.FirstOrDefault(Godot.Colors.Azure);
        var checkboxGroup = new ButtonGroup();
        foreach (var color in Colors)
        {
            var checkbox = new CheckBox()
            {
                Text = color.ToString(),
                ButtonGroup = checkboxGroup,
            };
            checkbox.Toggled += x =>
            {
                if (x)
                {
                    CurrentColor = color;
                }
            };
            CheckBoxContainer.AddChild(checkbox);
        }

        ElevationSlider.ValueChanged += SetElevation;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (
            @event is InputEventMouseButton mouseEvent
            && mouseEvent.Pressed
            && mouseEvent.ButtonIndex is MouseButton.Left
        )
        {
            // select cell
            var cell = GetCellFromScreen(mouseEvent.Position);
            if (cell is not null)
            {
                EditCell(cell);
            }
        }
        else if (@event is InputEventKey key && key.IsPressed() && key.Keycode == Key.D)
        {
            GetViewport().DebugDraw ^= Viewport.DebugDrawEnum.Wireframe;
        }
    }

    private HexCell GetCellFromScreen(Vector2 screen_pos)
    {
        const float RAY_LENGTH = 1000.0f;
        var mousePos = screen_pos;
        var camera = GetViewport().GetCamera3D();
        var origin = camera.ProjectRayOrigin(mousePos);
        var end = origin + (camera.ProjectRayNormal(mousePos) * RAY_LENGTH);

        var rayQuery = PhysicsRayQueryParameters3D.Create(origin, end);
        rayQuery.CollideWithAreas = true;

        var resultCell = HexGrid.GetCellFromRay(rayQuery);
        return resultCell;
    }

    public void SetElevation(double elevation)
    {
        CurrentElevation = Mathf.RoundToInt(elevation);
        SliderTextLabel.Text = elevation.ToString();
    }

    private void EditCell(HexCell cell)
    {
        cell.Color = CurrentColor;
        cell.Elevation = CurrentElevation;
        HexGrid.Refresh();
    }
}
