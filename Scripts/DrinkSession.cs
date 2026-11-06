// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BartenderSpecialogist;

public enum DrinkAction
{
    TakeShaker, Ice, Ingredient, Shake, Strain, Serve, CollectGlass, WashGlass
}
public enum IngredientKind
{
    Tequila, TripleSec, LimeJuice, SimpleSyrup, Vodka, Gin, Whiskey, Rum
}
public sealed record SpiritChoice(string Id, string Name, IngredientKind Ingredient);
public sealed record RecipeStep(string Id, DrinkAction Action, string Hint, IngredientKind? Ingredient = null);
public sealed record DrinkRecipe(string Id, string Name, RecipeStep[] Steps)
{
    public static DrinkRecipe Parse(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var recipe = JsonSerializer.Deserialize<DrinkRecipe>(json, options) ?? throw new ArgumentException("Recipe is empty.");
        if (string.IsNullOrWhiteSpace(recipe.Id) || string.IsNullOrWhiteSpace(recipe.Name) || recipe.Steps is not { Length: > 0 } ||
            recipe.Steps.Any(s => s == null || string.IsNullOrWhiteSpace(s.Id) || !Enum.IsDefined(s.Action) ||
                s.Action == DrinkAction.Ingredient && (s.Ingredient == null || !Enum.IsDefined(s.Ingredient.Value))) ||
            recipe.Steps.Select(s => s.Id).Distinct().Count() != recipe.Steps.Length)
            throw new ArgumentException("Recipes need a name, unique step IDs, and valid actions and ingredients.");
        return recipe;
    }
}

/// <summary>Recipe progress and real-time service lifecycle, independent of rendering/input.</summary>
public sealed class DrinkSession(DrinkRecipe recipe)
{
    private readonly HashSet<string> _done = [];
    private int _nextIndex;
    public DrinkRecipe Recipe { get; } = recipe;
    public bool Active { get; private set; }
    public bool Drinking { get; private set; }
    public bool HasEmptyGlass { get; private set; }
    public double DrinkingSeconds { get; private set; }
    public int DrinksFinished { get; private set; }
    public int Revision { get; private set; }
    public bool Asleep => DrinksFinished >= 5;
    public bool HasDone(DrinkAction action)
    {
        foreach (var step in Recipe.Steps)
            if (step.Action == action && _done.Contains(step.Id))
                return true;
        return false;
    }
    public bool StepDone(string id) => _done.Contains(id);

    public RecipeStep[] NextSteps
    {
        get
        {
            if (!Active || Drinking || _nextIndex >= Recipe.Steps.Length)
                return [];
            if (Recipe.Steps[_nextIndex].Action != DrinkAction.Ingredient)
                return [Recipe.Steps[_nextIndex]];
            return Recipe.Steps.Skip(_nextIndex).TakeWhile(s => s.Action == DrinkAction.Ingredient).Where(s => !_done.Contains(s.Id)).ToArray();
        }
    }

    public bool AcceptOrder()
    {
        if (Active || Asleep)
            return false;
        Active = true;
        Revision++;
        return true;
    }

    public bool Can(DrinkAction action, IngredientKind? ingredient = null) => FindNext(action, ingredient) != null;

    private RecipeStep? FindNext(DrinkAction action, IngredientKind? ingredient)
    {
        if (!Active || Drinking || _nextIndex >= Recipe.Steps.Length)
            return null;
        var ingredientBlock = Recipe.Steps[_nextIndex].Action == DrinkAction.Ingredient;
        for (var i = _nextIndex; i < Recipe.Steps.Length; i++)
        {
            var step = Recipe.Steps[i];
            if (i > _nextIndex && (!ingredientBlock || step.Action != DrinkAction.Ingredient))
                break;
            if (!_done.Contains(step.Id) && step.Action == action && (action != DrinkAction.Ingredient || step.Ingredient == ingredient))
                return step;
        }
        return null;
    }

    public bool Add(SpiritChoice bottle) => Complete(DrinkAction.Ingredient, bottle.Ingredient);

    public bool Complete(DrinkAction action, IngredientKind? ingredient = null)
    {
        var step = FindNext(action, ingredient);
        if (step == null)
            return false;
        _done.Add(step.Id);
        while (_nextIndex < Recipe.Steps.Length && _done.Contains(Recipe.Steps[_nextIndex].Id))
            _nextIndex++;
        if (action == DrinkAction.Serve)
        {
            Drinking = true;
            DrinkingSeconds = 0;
        }
        if (action == DrinkAction.WashGlass)
            ResetOrder();
        else
            Revision++;
        return true;
    }

    public bool GrabGlass()
    {
        if (!Can(DrinkAction.Strain) || HasEmptyGlass)
            return false;
        HasEmptyGlass = true;
        Revision++;
        return true;
    }

    public void Tick(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        if (!Drinking)
            return;
        DrinkingSeconds = Math.Min(CustomerDrinkCycle.Duration, DrinkingSeconds + seconds);
        if (DrinkingSeconds >= CustomerDrinkCycle.Duration)
        {
            Drinking = false;
            DrinksFinished++;
            Revision++;
        }
    }

    private void ResetOrder()
    {
        _done.Clear();
        _nextIndex = 0;
        Active = Drinking = HasEmptyGlass = false;
        DrinkingSeconds = 0;
        Revision++;
    }
    public void ResetShift()
    {
        ResetOrder();
        DrinksFinished = 0;
    }
}

public static class CustomerDrinkCycle
{
    public const double Duration = 60;
    private static readonly float[] SipStarts = [0, 12, 22, 32, 42, 52];
    public static float Fill(double seconds)
    {
        var finished = 0;
        foreach (var start in SipStarts)
            if (seconds >= start + 4)
                finished++;
        return Math.Clamp(1 - finished / 6f, 0, 1);
    }
    public static PatronMotion.SipPose Pose(double seconds)
    {
        var start = 0f;
        foreach (var sip in SipStarts)
        {
            if (seconds < sip)
                break;
            start = sip;
        }
        var t = (float)seconds - start;
        return new(PatronMotion.Window(t, 0, .75f, 5.8f, 6.5f),
            PatronMotion.Window(t, .6f, .9f, 5.5f, 6), PatronMotion.Window(t, 1, 2.5f, 4, 5.5f));
    }
}
