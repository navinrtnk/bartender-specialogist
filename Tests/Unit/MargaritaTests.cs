// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Xunit;

namespace BartenderSpecialogist.Tests;

public class MargaritaTests
{
    private static DrinkSession Order()
    {
        var recipe = DrinkRecipe.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Recipes", "margarita.json")));
        var session = new DrinkSession(recipe);
        Assert.True(session.AcceptOrder());
        return session;
    }

    private static void Mix(DrinkSession session, string brand = "House tequila", bool strain = true)
    {
        Assert.True(session.Complete(DrinkAction.TakeShaker));
        Assert.True(session.Complete(DrinkAction.Ice));
        Assert.True(session.Add(new(brand, brand, IngredientKind.Tequila)));
        Assert.True(session.Add(new("triple-sec", "Triple sec", IngredientKind.TripleSec)));
        Assert.True(session.Complete(DrinkAction.Ingredient, IngredientKind.LimeJuice));
        Assert.True(session.Complete(DrinkAction.Ingredient, IngredientKind.SimpleSyrup));
        Assert.True(session.Complete(DrinkAction.Shake));
        if (strain)
            Assert.True(session.Complete(DrinkAction.Strain));
    }

    [Theory]
    [InlineData("House tequila")]
    [InlineData("Patrón Silver")]
    [InlineData("Jose Cuervo Especial")]
    [InlineData("Don Julio Reposado")]
    [InlineData("Añejo from an unknown brand")]
    [InlineData("Any custom blanco tequila")]
    public void MargaritaAcceptsAnyTequilaBrandOrStyle(string tequila)
    {
        var session = Order();
        Mix(session, tequila);
        Assert.True(session.Can(DrinkAction.Serve));
    }

    [Theory]
    [InlineData(IngredientKind.Vodka)]
    [InlineData(IngredientKind.Gin)]
    [InlineData(IngredientKind.Whiskey)]
    [InlineData(IngredientKind.Rum)]
    public void OtherSpiritsCannotSubstituteForTequila(IngredientKind ingredient)
    {
        var session = Order();
        session.Complete(DrinkAction.TakeShaker);
        session.Complete(DrinkAction.Ice);
        Assert.False(session.Add(new("wrong", "Wrong liquor", ingredient)));
        Assert.True(session.Can(DrinkAction.Ingredient, IngredientKind.Tequila));
    }

    [Fact]
    public void RecipeRequiresAllFourIngredientsButAllowsTheirOrderToVary()
    {
        var session = Order();
        session.Complete(DrinkAction.TakeShaker);
        session.Complete(DrinkAction.Ice);
        var ingredients = session.NextSteps.Select(s => s.Ingredient).ToHashSet();
        Assert.Equal(4, ingredients.Count);
        foreach (var ingredient in new[] { IngredientKind.SimpleSyrup, IngredientKind.LimeJuice, IngredientKind.TripleSec })
            Assert.True(session.Complete(DrinkAction.Ingredient, ingredient));
        Assert.False(session.Can(DrinkAction.Shake));
        Assert.True(session.Complete(DrinkAction.Ingredient, IngredientKind.Tequila));
        Assert.True(session.Can(DrinkAction.Shake));
        Assert.False(session.Complete(DrinkAction.Ingredient, IngredientKind.Tequila));
    }

    [Fact]
    public void StepsCannotSkipIceShakingOrStraining()
    {
        var session = Order();
        Assert.False(session.Complete(DrinkAction.Shake));
        Assert.False(session.Complete(DrinkAction.Serve));
        session.Complete(DrinkAction.TakeShaker);
        Assert.False(session.Complete(DrinkAction.Ingredient, IngredientKind.Tequila));
        Assert.False(session.GrabGlass());
        Assert.True(session.Complete(DrinkAction.Ice));
        foreach (var ingredient in new[] { IngredientKind.Tequila, IngredientKind.TripleSec, IngredientKind.LimeJuice, IngredientKind.SimpleSyrup })
            Assert.True(session.Complete(DrinkAction.Ingredient, ingredient));
        Assert.False(session.Complete(DrinkAction.Strain));
        Assert.False(session.Complete(DrinkAction.Serve));
        Assert.True(session.Complete(DrinkAction.Shake));
        Assert.False(session.Complete(DrinkAction.Serve));
        Assert.True(session.Complete(DrinkAction.Strain));
        Assert.True(session.Complete(DrinkAction.Serve));
    }

    [Theory]
    [InlineData(IngredientKind.Tequila)]
    [InlineData(IngredientKind.TripleSec)]
    [InlineData(IngredientKind.LimeJuice)]
    [InlineData(IngredientKind.SimpleSyrup)]
    public void EachRequiredIngredientMustBePresent(IngredientKind omitted)
    {
        var session = Order();
        session.Complete(DrinkAction.TakeShaker);
        session.Complete(DrinkAction.Ice);
        foreach (var ingredient in new[] { IngredientKind.Tequila, IngredientKind.TripleSec, IngredientKind.LimeJuice, IngredientKind.SimpleSyrup }.Except([omitted]))
            Assert.True(session.Complete(DrinkAction.Ingredient, ingredient));
        Assert.False(session.Complete(DrinkAction.Shake));
        Assert.False(session.Complete(DrinkAction.Strain));
        Assert.False(session.Complete(DrinkAction.Serve));
        Assert.Equal(omitted, Assert.Single(session.NextSteps).Ingredient);
    }

    [Fact]
    public void GrabbingAGlassIsOptionalAndCannotDuplicateIt()
    {
        var session = Order();
        Mix(session);
        Assert.False(session.GrabGlass());
        session = Order();
        Mix(session, strain: false);
        Assert.True(session.GrabGlass());
        Assert.False(session.GrabGlass());
        Assert.True(session.Complete(DrinkAction.Strain));
        Assert.True(session.Can(DrinkAction.Serve));
    }

    [Fact]
    public void DrinkTakesOneRealMinuteAndLeavesAnEmptyGlass()
    {
        var session = Order();
        Mix(session);
        session.Complete(DrinkAction.Serve);
        Assert.False(session.Complete(DrinkAction.CollectGlass));
        session.Tick(7);
        Assert.InRange(CustomerDrinkCycle.Fill(session.DrinkingSeconds), 0, .99f);
        session.Tick(52.9);
        Assert.True(session.Drinking);
        session.Tick(.1);
        Assert.False(session.Drinking);
        Assert.Equal(0, CustomerDrinkCycle.Fill(session.DrinkingSeconds));
        Assert.True(session.Complete(DrinkAction.CollectGlass));
        Assert.True(session.Complete(DrinkAction.WashGlass));
        Assert.False(session.Active);
        Assert.True(session.AcceptOrder());
    }

    [Fact]
    public void FifthFinishedDrinkEndsTheShiftAndResetAllowsAnotherLoop()
    {
        var session = Order();
        for (var drink = 1; drink <= 5; drink++)
        {
            Mix(session);
            session.Complete(DrinkAction.Serve);
            session.Tick(60);
            Assert.Equal(drink, session.DrinksFinished);
            session.Complete(DrinkAction.CollectGlass);
            session.Complete(DrinkAction.WashGlass);
            Assert.Equal(drink == 5, session.Asleep);
            Assert.Equal(drink < 5, session.AcceptOrder());
        }
        session.ResetShift();
        Assert.True(session.AcceptOrder());
        Assert.Equal(0, session.DrinksFinished);
    }

    [Fact]
    public void ActiveOrderCannotBeAcceptedTwice()
    {
        var session = Order();
        Assert.False(session.AcceptOrder());
        Assert.Single(session.NextSteps);
        Assert.Equal(DrinkAction.TakeShaker, session.NextSteps[0].Action);
    }

    [Fact]
    public void InvalidRecipeIdsAreRejected() => Assert.Throws<ArgumentException>(() =>
        DrinkRecipe.Parse("""{"id":"bad","name":"Bad","steps":[{"id":"same","action":"Ice"},{"id":"same","action":"Shake"}]}"""));

    [Theory]
    [InlineData("""{"id":"bad","name":"Bad","steps":[null]}""")]
    [InlineData("""{"id":"bad","name":"Bad","steps":[]}""")]
    [InlineData("""{"id":"bad","name":"Bad","steps":[{"id":"x","action":99}]}""")]
    [InlineData("""{"id":"bad","name":"Bad","steps":[{"id":"x","action":"Ingredient","ingredient":99}]}""")]
    [InlineData("""{"id":"bad","name":"Bad","steps":[{"id":"x","action":"Ingredient"}]}""")]
    [InlineData("""{"id":"bad","steps":[{"id":"x","action":"Ice"}]}""")]
    public void MalformedRecipeDefinitionsAreRejected(string json) =>
        Assert.Throws<ArgumentException>(() => DrinkRecipe.Parse(json));

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidTimeCannotChangeDrinkProgress(double seconds)
    {
        var session = Order();
        Mix(session);
        session.Complete(DrinkAction.Serve);
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Tick(seconds));
        Assert.True(session.Drinking);
        Assert.Equal(0, session.DrinkingSeconds);
        Assert.Equal(0, session.DrinksFinished);
    }

    [Fact]
    public void ProgressAndHintsAdvanceThroughServiceCleanupAndNextOrder()
    {
        var session = Order();
        Mix(session);
        session.Complete(DrinkAction.Serve);
        Assert.Empty(session.NextSteps);
        var revision = session.Revision;
        session.Tick(59.99);
        Assert.Equal(revision, session.Revision);
        Assert.Empty(session.NextSteps);
        session.Tick(1);
        Assert.True(session.Revision > revision);
        Assert.Equal(DrinkAction.CollectGlass, Assert.Single(session.NextSteps).Action);
        session.Complete(DrinkAction.CollectGlass);
        Assert.Equal(DrinkAction.WashGlass, Assert.Single(session.NextSteps).Action);
        session.Complete(DrinkAction.WashGlass);
        Assert.Empty(session.NextSteps);
        Assert.False(session.StepDone("tequila"));
        Assert.False(session.HasDone(DrinkAction.Serve));
        session.AcceptOrder();
        Assert.Equal(DrinkAction.TakeShaker, Assert.Single(session.NextSteps).Action);
    }

    [Fact]
    public void FrameQueriesDoNotAllocateTemporaryRecipeLists()
    {
        var session = Order();
        session.Complete(DrinkAction.TakeShaker);
        session.Complete(DrinkAction.Ice);
        int Query()
        {
            var canPour = session.Can(DrinkAction.Ingredient, IngredientKind.Tequila);
            var served = session.HasDone(DrinkAction.Serve);
            var fill = CustomerDrinkCycle.Fill(17);
            var pose = CustomerDrinkCycle.Pose(17);
            return canPour && !served && fill < 1 && pose.Grip >= 0 ? 1 : 0;
        }
        for (var i = 0; i < 100; i++)
            Query();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var allowed = 0;
        for (var i = 0; i < 1000; i++)
            allowed += Query();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(1000, allowed);
        Assert.InRange(allocated, 0, 1024);
    }
}
