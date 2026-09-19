using System.Collections.Generic;
using System.Linq;
using PullMats.Core;
using Xunit;

namespace PullMats.Tests
{
    public class PullPlannerTests
    {
        private static readonly MaterialNeed Wood = new("$item_wood", "Wood", 10, 2f, 50);
        private static readonly MaterialNeed Core = new("$item_surtlingcore", "Surtling core", 2, 1f, 20);

        private static PlayerState Player(
            Dictionary<string, int>? carried = null,
            float weight = 0f,
            float maxWeight = 300f,
            int emptySlots = 32,
            Dictionary<string, int>? freeStack = null) =>
            new(carried ?? new(), weight, maxWeight, emptySlots, freeStack ?? new());

        private static PullRequest Request(
            IReadOnlyList<MaterialNeed> needs,
            IReadOnlyList<SourceStock> stock,
            PlayerState? player = null,
            PullMode mode = PullMode.FullSet) =>
            new("$piece_test", mode, needs, stock, player ?? Player());

        private static List<SourceStock> Plenty(params MaterialNeed[] needs) =>
            needs.Select(n => new SourceStock(0, n.Key, 1000)).ToList();

        private static Dictionary<string, int> AmountsOf(PullResult result) =>
            Assert.IsType<PullResult.Success>(result).Totals.ToDictionary(t => t.Need.Key, t => t.Amount);

        [Fact]
        public void FullSet_PullsExactlyPerSet_EvenWhenAlreadyCarrying()
        {
            var player = Player(carried: new() { [Wood.Key] = 4, [Core.Key] = 1 });
            var amounts = AmountsOf(PullPlanner.Plan(Request(new[] { Wood, Core }, Plenty(Wood, Core), player)));

            Assert.Equal(10, amounts[Wood.Key]);
            Assert.Equal(2, amounts[Core.Key]);
        }

        [Fact]
        public void FullSet_ThreePressesInARow_GiveThreeSets()
        {
            var carried = new Dictionary<string, int> { [Wood.Key] = 3 };
            for (int press = 0; press < 3; press++)
            {
                var amounts = AmountsOf(PullPlanner.Plan(Request(new[] { Wood }, Plenty(Wood), Player(carried: carried))));
                carried[Wood.Key] += amounts[Wood.Key];
            }

            Assert.Equal(3 + 30, carried[Wood.Key]);
        }

        [Theory]
        [InlineData(4, 6)]
        [InlineData(10, 10)]
        [InlineData(0, 10)]
        [InlineData(25, 5)]
        public void TopUp_RoundsUpToNextWholeSet(int carried, int expected)
        {
            var player = Player(carried: new() { [Wood.Key] = carried });
            var amounts = AmountsOf(PullPlanner.Plan(Request(new[] { Wood }, Plenty(Wood), player, PullMode.TopUp)));

            Assert.Equal(expected, amounts[Wood.Key]);
        }

        [Fact]
        public void TopUp_EachMaterialRoundsIndependently()
        {
            var player = Player(carried: new() { [Wood.Key] = 4 });
            var amounts = AmountsOf(PullPlanner.Plan(Request(new[] { Wood, Core }, Plenty(Wood, Core), player, PullMode.TopUp)));

            Assert.Equal(6, amounts[Wood.Key]);
            Assert.Equal(2, amounts[Core.Key]);
        }

        [Fact]
        public void TopUp_OverOneSetWithPartialOther()
        {
            var player = Player(carried: new() { [Wood.Key] = 25, [Core.Key] = 1 });
            var amounts = AmountsOf(PullPlanner.Plan(Request(new[] { Wood, Core }, Plenty(Wood, Core), player, PullMode.TopUp)));

            Assert.Equal(5, amounts[Wood.Key]);
            Assert.Equal(1, amounts[Core.Key]);
        }

        [Fact]
        public void Missing_ReportsEveryShortMaterial()
        {
            var stock = new List<SourceStock>
            {
                new(0, Wood.Key, 4),
                new(1, Wood.Key, 3),
                new(0, Core.Key, 1),
            };
            var result = Assert.IsType<PullResult.Missing>(PullPlanner.Plan(Request(new[] { Wood, Core }, stock)));

            Assert.Collection(result.Shortfalls,
                s => { Assert.Equal(Wood, s.Need); Assert.Equal(7, s.Available); Assert.Equal(10, s.Required); },
                s => { Assert.Equal(Core, s.Need); Assert.Equal(1, s.Available); Assert.Equal(2, s.Required); });
        }

        [Fact]
        public void Missing_WhenMaterialAbsentEntirely()
        {
            var result = Assert.IsType<PullResult.Missing>(PullPlanner.Plan(Request(new[] { Wood, Core }, Plenty(Wood))));

            var shortfall = Assert.Single(result.Shortfalls);
            Assert.Equal(Core, shortfall.Need);
            Assert.Equal(0, shortfall.Available);
        }

        [Fact]
        public void Weight_ExactlyAtMax_IsAllowed()
        {
            // one set of wood weighs 20
            var player = Player(weight: 280f, maxWeight: 300f);
            Assert.IsType<PullResult.Success>(PullPlanner.Plan(Request(new[] { Wood }, Plenty(Wood), player)));
        }

        [Fact]
        public void Weight_OverMax_IsTooHeavy()
        {
            var player = Player(weight: 280.1f, maxWeight: 300f);
            var result = Assert.IsType<PullResult.TooHeavy>(PullPlanner.Plan(Request(new[] { Wood }, Plenty(Wood), player)));

            Assert.Equal(300.1f, result.ResultingWeight, 3);
            Assert.Equal(300f, result.MaxWeight);
        }

        [Fact]
        public void Space_NoEmptySlotsAndNoStackRoom_IsNoSpace()
        {
            var player = Player(emptySlots: 0);
            var result = Assert.IsType<PullResult.NoSpace>(PullPlanner.Plan(Request(new[] { Wood }, Plenty(Wood), player)));

            Assert.Equal(1, result.SlotsNeeded);
            Assert.Equal(0, result.SlotsFree);
        }

        [Fact]
        public void Space_ExistingStacksAbsorbEverything_Passes()
        {
            var player = Player(emptySlots: 0, freeStack: new() { [Wood.Key] = 10, [Core.Key] = 5 });
            Assert.IsType<PullResult.Success>(PullPlanner.Plan(Request(new[] { Wood, Core }, Plenty(Wood, Core), player)));
        }

        [Fact]
        public void Space_CountsSlotsPerMaterialAgainstMaxStack()
        {
            var bigWood = Wood with { PerSet = 120 }; // 120 wood, max stack 50 -> 3 slots
            var player = Player(emptySlots: 3, maxWeight: 10000f);
            Assert.IsType<PullResult.Success>(PullPlanner.Plan(Request(new[] { bigWood }, Plenty(bigWood), player)));

            var tight = Player(emptySlots: 2, maxWeight: 10000f);
            var result = Assert.IsType<PullResult.NoSpace>(PullPlanner.Plan(Request(new[] { bigWood }, Plenty(bigWood), tight)));
            Assert.Equal(3, result.SlotsNeeded);
        }

        [Fact]
        public void Allocation_SpansSourcesInIndexOrder_NeverOverdraws()
        {
            var stock = new List<SourceStock>
            {
                new(2, Wood.Key, 100),
                new(0, Wood.Key, 4),
                new(1, Wood.Key, 3),
            };
            var success = Assert.IsType<PullResult.Success>(PullPlanner.Plan(Request(new[] { Wood }, stock)));

            Assert.Collection(success.Takes,
                t => Assert.Equal(new Take(0, Wood.Key, 4), t),
                t => Assert.Equal(new Take(1, Wood.Key, 3), t),
                t => Assert.Equal(new Take(2, Wood.Key, 3), t));
        }

        [Fact]
        public void NoRequirements_IsNothingToPull()
        {
            Assert.IsType<PullResult.NothingToPull>(PullPlanner.Plan(Request(new MaterialNeed[0], new List<SourceStock>())));
        }

        [Fact]
        public void AllZeroPerSet_IsNothingToPull()
        {
            var needs = new[] { Wood with { PerSet = 0 }, Core with { PerSet = 0 } };
            Assert.IsType<PullResult.NothingToPull>(PullPlanner.Plan(Request(needs, Plenty(Wood, Core))));
        }

        [Fact]
        public void MixedZeroPerSet_IgnoresZeroOnes_InTopUpToo()
        {
            var needs = new[] { Wood, Core with { PerSet = 0 } };
            var amounts = AmountsOf(PullPlanner.Plan(Request(needs, Plenty(Wood), mode: PullMode.TopUp)));

            Assert.Equal(new Dictionary<string, int> { [Wood.Key] = 10 }, amounts);
        }

        [Fact]
        public void DuplicateRequirements_AreMerged()
        {
            var needs = new[] { Wood, Wood with { PerSet = 5 } };
            var amounts = AmountsOf(PullPlanner.Plan(Request(needs, Plenty(Wood))));

            Assert.Equal(15, amounts[Wood.Key]);
        }
    }
}
