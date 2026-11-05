// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;

namespace BartenderSpecialogist;

/// <summary>Composition root for the neighborhood bar and recessed working station.</summary>
public partial class BarScene : Node3D
{
    public override void _Ready()
    {
        GetViewport().UseTaa = RenderingServer.GetCurrentRenderingMethod() == "forward_plus";
        var p = new ScenePalette();
        AddLighting();
        BarInterior.Build(this, p);
        BackBar.Build(this, p);
        MargaritaStation.Build(this, p);
        var customer = new SeatedCustomer();
        AddChild(customer);
        customer.Build(p);
        var player = new FirstPersonController();
        AddChild(player);
        var hud = new SceneHud();
        AddChild(hud);
        hud.Build(player);
        PatronGroups.Build(this,p);
        SceneDiagnostics.Run(this, player, hud);
    }

    private void AddLighting()
    {
        var desktop = RenderingServer.GetCurrentRenderingMethod() == "forward_plus";
        var skyMaterial = new ProceduralSkyMaterial
        {
            SkyTopColor = new Color("aecbd8"),
            SkyHorizonColor = new Color("edf0e7"),
            GroundBottomColor = new Color("8a877c"),
            GroundHorizonColor = new Color("e1dfd3")
        };
        AddChild(new WorldEnvironment
        {
            Name = "DaylightEnvironment",
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Sky,
                Sky = new Sky { SkyMaterial = skyMaterial },
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color("e3e9ed"),
                AmbientLightEnergy = .33f,
                ReflectedLightSource = Godot.Environment.ReflectionSource.Sky,
                TonemapMode = Godot.Environment.ToneMapper.Aces,
                TonemapExposure = 1.1f,
                SsaoEnabled = true,
                SsaoRadius = .22f,
                SsaoIntensity = 1.5f,
                SsaoPower = 1.4f,
                SsaoDetail = .8f,
                SsilEnabled = desktop,
                SsilRadius = 3,
                SsilIntensity = .65f,
                SsrEnabled = desktop,
                SsrMaxSteps = 48
            }
        });
        AddChild(new DirectionalLight3D
        {
            Name = "AfternoonSun",
            RotationDegrees = new(-32, 48, 0),
            LightColor = new Color("fff3db"),
            LightEnergy = 1.1f,
            ShadowEnabled = true,
            DirectionalShadowMaxDistance = 24,
            ShadowBlur = 2.5f,
            ShadowBias = .015f,
            LightAngularDistance = 2
        });
        foreach (var z in new[] { -5.5f, -.5f, 3.2f })
            AddChild(new OmniLight3D
            {
                Name = "SoftInteriorFill",
                Position = new(0, 2.85f, z),
                LightColor = new Color("fff6e9"),
                LightEnergy = 1.15f,
                LightSpecular = .15f,
                OmniRange = 8,
                OmniAttenuation = 1.3f,
                ShadowEnabled = z == -.5f,
                ShadowBlur = 2
            });
        AddChild(new ReflectionProbe
        {
            Name = "BarInteriorReflections",
            Position = new(0, 1.65f, -1.9f),
            Size = new(11.8f, 3.4f, 13.4f),
            Interior = true,
            BoxProjection = true,
            Intensity = .8f,
            MaxDistance = 20,
            UpdateMode = ReflectionProbe.UpdateModeEnum.Once
        });
    }
}
