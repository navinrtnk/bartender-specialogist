// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;

namespace BartenderSpecialogist.Tests;

public static class GameplayTests
{
    public static async Task Run(Node3D scene, FirstPersonController player)
    {
        var game = scene.GetNode<DrinkGameplay>("DrinkGameplay");
        var customer = scene.GetNode<SeatedCustomer>("SeatedCustomer");
        game.SetProcess(false);
        player.SetProcess(false);
        customer.SetProcess(false);
        var capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--gameplay-capture="))?.Split('=', 2)[1];
        if (capture != null)
            System.IO.Directory.CreateDirectory(capture);
        var time = 0f;
        var preview = OS.GetCmdlineUserArgs().Contains("--preview-animations");
        var previewServing = OS.GetCmdlineUserArgs().Contains("--preview-serving");
        var recorded = new HashSet<string>();
        if (preview || previewServing)
            foreach (var patron in scene.GetNode("BackgroundPatrons").GetChildren().OfType<PatronActor>())
                patron.SetProcess(true);
        void Check(string name, bool valid)
        {
            if (!valid)
                throw new InvalidOperationException("Gameplay test failed: " + name);
            GD.Print("PASS gameplay/" + name);
        }
        async Task Keypress(Key key)
        {
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = true });
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = false });
            for (var i = 0; i < 2; i++)
                await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame);
            game.Advance(0);
        }
        void Aim(string id)
        {
            var target = game.Targets.Single(t => t.Id == id);
            player.Position = id == "customer"
                ? game.Session.Can(DrinkAction.Serve) || game.Session.Can(DrinkAction.CollectGlass)
                    ? new(3.45f, 1.72f, .82f) : FirstPersonController.StartPosition
                : id == "well" ? new(.9f, 1.72f, 2.15f) : new(target.Center().X, 1.72f, 1.78f);
            player.Camera.LookAt(target.Center(), Vector3.Up);
            game.Advance(0);
            Check("aim highlights " + id, game.Highlighted?.Id == id);
        }
        async Task Sample(string name)
        {
            var hands = player.Camera.GetNode<BartenderHands>("BartenderHands");
            if (name is "ice" or "tequila" or "triple-sec" or "lime" or "syrup" or "shake" or "pour")
            {
                var rig = hands.FindChildren("*", "Skeleton3D", true, false).OfType<Skeleton3D>().Single();
                var wrist = rig.GetBoneGlobalPose(rig.FindBone("wrist_R"));
                var thumb = wrist.AffineInverse() * rig.GetBoneGlobalPose(rig.FindBone("finger1-3_R")).Origin;
                Check(name + " thumb closes across palm rather than into wrist", thumb.Z > .08f && thumb.Y > -.06f);
                if (name == "ice")
                {
                    var scoop = (Node3D)scene.FindChild("IceScoop", true, false);
                    var thumbDirection = rig.GlobalBasis * wrist.Basis * Vector3.Right;
                    Check("right scoop thumb points toward the bowl", thumbDirection.Dot(-scoop.GlobalBasis.Z) > .95f);
                }
            }
            if (name == "cap-grip")
            {
                var rig = hands.FindChildren("*", "Skeleton3D", true, false).OfType<Skeleton3D>().Single();
                var thumbDirection = rig.GlobalBasis * rig.GetBoneGlobalPose(rig.FindBone("wrist_R")).Basis * Vector3.Right;
                Check("right thumb stays upright while fitting the cap", thumbDirection.Dot(Vector3.Up) > .8f);
            }
            GD.Print($"GRIP_CHECK {name} left={hands.LeftGripError:F4} right={hands.RightGripError:F4}");
            if (name is "ice" or "tequila" or "triple-sec" or "lime" or "syrup" or "shake" or "pour")
                Check(name + " wrist contact", hands.LeftGripError < .025f && hands.RightGripError < .025f);
            if (capture == null)
                return;
            for (var i = 0; i < 12; i++)
                await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame);
            await scene.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var image = scene.GetViewport().GetTexture().GetImage();
            var result = image.SavePng(System.IO.Path.Combine(capture, name + ".png"));
            Check("capture " + name, result == Error.Ok);
        }
        async Task Finish(string name, float sampleAt)
        {
            var sampled = false;
            var realtime = (preview && (name is "shake" or "pour") || previewServing && name == "serve") && recorded.Add(name);
            if (realtime)
                GD.Print($"PREVIEW_BEGIN {name} {Engine.GetProcessFrames()}");
            for (var frame = 0; game.Actions.Busy && frame < 1200; frame++)
            {
                time += 1f / 60;
                game.Advance(1f / 60);
                customer.Evaluate(time, 1f / 60);
                if (name == "shake" && frame == 45)
                    await Sample("cap-grip");
                if (!sampled && frame / 60f >= sampleAt)
                {
                    sampled = true;
                    await Sample(name);
                }
                if (realtime)
                    await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            if (realtime)
                GD.Print($"PREVIEW_END {name} {Engine.GetProcessFrames()}");
            Check(name + " action completes", !game.Actions.Busy);
        }
        async Task DrinkFor(float seconds, float poseDelta)
        {
            if (!previewServing)
            {
                game.Advance(seconds);
                customer.Evaluate(time += seconds, poseDelta);
                return;
            }
            for (var frame = 0; frame < (int)MathF.Round(seconds * 60); frame++)
            {
                game.Advance(1f / 60);
                customer.Evaluate(time += 1f / 60, 1f / 60);
                await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }
        async Task Make(bool grabFirst)
        {
            Aim("shaker");
            await Keypress(Key.Space);
            await Finish("shaker", 1.0f);
            Check("shaker held", game.Actions.HoldingShaker);
            Aim("ice");
            await Keypress(Key.Space);
            await Finish("ice", 1.8f);
            Aim("well");
            await Keypress(Key.Space);
            await Keypress(Key.Space);
            await Finish("tequila", 2.3f);
            Check("well reopens after a pour", game.Hud.MenuOpen);
            await Keypress(Key.Up);
            await Keypress(Key.Space);
            await Finish("triple-sec", 2.3f);
            await Keypress(Key.Escape);
            Check("escape closes the bottle selection", !game.Hud.MenuOpen);
            Aim("LimeJuice");
            await Keypress(Key.Space);
            await Finish("lime", 2.3f);
            Aim("SimpleSyrup");
            await Keypress(Key.Space);
            await Finish("syrup", 2.3f);
            Check("all four ingredients added", game.Session.Can(DrinkAction.Shake));
            await Keypress(Key.Space);
            await Finish("shake", 3);
            Aim("glasses");
            await Keypress(Key.Space);
            if (grabFirst)
            {
                await Keypress(Key.Space);
                await Finish("grab-glass", 1.1f);
                Check("grab glass menu option works", game.Actions.HoldingGlass && game.Session.HasEmptyGlass);
                Aim("glasses");
                await Keypress(Key.Space);
            }
            await Keypress(Key.Down);
            await Keypress(Key.Space);
            await Finish("pour", 2.8f);
            Check("margarita ready to serve", game.Session.Can(DrinkAction.Serve) && game.Actions.HoldingGlass);
        }
        player.Position = new(-3.6f, 1.72f, 3.22f);
        player.Camera.LookAt(customer.HeadPosition, Vector3.Up);
        game.Advance(0);
        Check("customer cannot be selected from outside talking distance", game.Highlighted?.Id != "customer");
        Aim("customer");
        Check("customer order is reachable from the workstation", player.Position == FirstPersonController.StartPosition);
        await Keypress(Key.Space);
        Check("order dialogue opens", game.Hud.MenuOpen);
        await Keypress(Key.Right);
        await Keypress(Key.Space);
        Check("declining leaves the order inactive", !game.Session.Active);
        Aim("customer");
        await Keypress(Key.Space);
        await Keypress(Key.Space);
        Check("accepting starts recipe", game.Session.Active);
        Check("recipe hint appears after accepting the order", game.Hud.RecipeText.Contains("○ Pick up the shaker"));
        await Make(false);
        player.Position = FirstPersonController.StartPosition;
        player.Camera.LookAt(customer.HeadPosition, Vector3.Up);
        game.Advance(0);
        Check("serving still requires a physical reach", game.Highlighted?.Id != "customer");
        Aim("customer");
        await Keypress(Key.Space);
        await Finish("serve", 1.7f);
        Check("customer drinking", game.Session.Drinking);
        Check("recipe hints update after serving", game.Hud.RecipeText.Contains("Customer is enjoying the drink"));
        await DrinkFor(2.8f, 2.8f);
        await Sample("customer-sip");
        await DrinkFor(5, 1f / 60);
        Check("great drink speech and audio", game.Hud.SpeechVisible && game.Hud.SpeechText == "Great drink!" && game.FeedbackPlaying);
        await Sample("great-drink");
        if (previewServing)
        {
            await DrinkFor(1, 1f / 60);
            GD.Print($"PREVIEW_END service {Engine.GetProcessFrames()}");
        }
        game.Advance(60);
        customer.Evaluate(time += 60, 1f / 60);
        Check("empty glass after a minute", game.Session.Can(DrinkAction.CollectGlass) && CustomerDrinkCycle.Fill(game.Session.DrinkingSeconds) == 0);
        Check("recipe hint advances when the customer finishes", game.Hud.RecipeText.Contains("Collect the empty glass"));
        Check("only ice remains", !game.Actions.Glass!.GetNode<MeshInstance3D>("LimeCocktail").Visible && game.Actions.Glass.GetNode<Node3D>("ServingIce").Visible);
        Aim("customer");
        await Keypress(Key.Space);
        await Finish("collect", 1.2f);
        Aim("dishwasher");
        await Keypress(Key.Space);
        await Finish("dishwasher", 2);
        Check("cleanup resets the order", !game.Session.Active && game.Actions.Glass == null && !game.Actions.HoldingShaker);
        Check("recipe hints reset after cleanup", game.Hud.RecipeText.Contains("take an order from the workstation"));
        Aim("customer");
        await Keypress(Key.Space);
        await Keypress(Key.Space);
        Check("next order can start", game.Session.Active);
        await Make(true);
        Aim("customer");
        await Keypress(Key.Space);
        await Finish("second-serve", 1.7f);
        var lastGlass = game.Actions.Glass!;
        for (var drink = 2; drink <= 5; drink++)
        {
            if (!game.Session.HasDone(DrinkAction.Serve))
            {
                game.Session.Complete(DrinkAction.TakeShaker);
                game.Session.Complete(DrinkAction.Ice);
                foreach (var ingredient in new[] { IngredientKind.Tequila, IngredientKind.TripleSec, IngredientKind.LimeJuice, IngredientKind.SimpleSyrup })
                    game.Session.Complete(DrinkAction.Ingredient, ingredient);
                game.Session.Complete(DrinkAction.Shake);
                game.Session.Complete(DrinkAction.Strain);
            }
            if (!game.Session.HasDone(DrinkAction.Serve))
                game.Session.Complete(DrinkAction.Serve);
            customer.ReceiveGlass(lastGlass);
            game.Session.Tick(60);
            for (var frame = 0; frame < 180; frame++)
                customer.Evaluate(time += 1f / 60, 1f / 60);
            if (drink < 5)
            {
                customer.ReleaseGlass();
                game.Session.Complete(DrinkAction.CollectGlass);
                game.Session.Complete(DrinkAction.WashGlass);
                game.Session.AcceptOrder();
            }
        }
        GD.Print($"PASSOUT_HEAD y={customer.HeadPosition.Y:F3}");
        Check("fifth finished drink passes the customer out", game.Session.Asleep && customer.HeadPosition.Y > 1.15f && customer.HeadPosition.Y < 1.45f);
        await Sample("asleep");
        Aim("customer");
        await Keypress(Key.Space);
        await Finish("last-collect", 1.2f);
        Aim("dishwasher");
        await Keypress(Key.Space);
        await Finish("last-wash", 2);
        Check("sleeping customer cannot order before shift reset", game.Session.Asleep && !game.Session.AcceptOrder());
        Aim("customer");
        await Keypress(Key.Space);
        await Keypress(Key.Space);
        for (var i = 0; i < 150; i++)
            customer.Evaluate(time += 1f / 60, 1f / 60);
        Check("new shift resets intoxication and wakes customer", !game.Session.Asleep && game.Session.DrinksFinished == 0 && customer.HeadPosition.Y > 1.5f);
        Aim("customer");
        await Keypress(Key.Space);
        await Keypress(Key.Space);
        Check("orders can repeat after a shift reset", game.Session.Active);
        game.StopFeedback();
        await scene.ToSignal(scene.GetTree().CreateTimer(.12), SceneTreeTimer.SignalName.Timeout);
        GD.Print("GAMEPLAY_TESTS complete");
    }
}
