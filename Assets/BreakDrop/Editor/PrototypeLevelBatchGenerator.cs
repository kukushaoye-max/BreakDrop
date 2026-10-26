using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BreakDrop.Editor
{
    public static class PrototypeLevelBatchGenerator
    {
        internal const float CenterX = 0.32692308f;
        internal const float BaseY = -2.76923077f;
        private const float GoalY = -3.66923077f;
        private const int MaxAttempts = 50;

        private static readonly string[] ArchetypeNames =
        {
            "Stepped Wall", "Left L Pocket", "Right L Pocket", "Central Shaft", "Offset Crown",
            "Single Arch", "S Stair", "Twin Columns", "Zigzag Shaft", "Nested L",
            "Offset Cantilever", "Crossed Wall", "Double Cavity", "Asymmetric Bridge", "Funnel",
            "Double L Room", "Spiral Supports", "Triple Weave", "Inverted Crown", "Compound Fortress"
        };

        [MenuItem("Tools/Break & Drop/Generate 20 Level Set")]
        public static void Generate()
        {
            var generated = new List<LevelDefinition>(20);
            var signatures = new HashSet<string>();
            for (int playLevel = 1; playLevel <= 20; playLevel++)
            {
                LevelDefinition accepted = null;
                string rejection = "no candidate";
                for (int attempt = 0; attempt < MaxAttempts; attempt++)
                {
                    LevelDefinition candidate;
                    if (!TryCreateCandidate(playLevel, attempt, out candidate, out rejection)) continue;
                    string signature = LayoutSignature(candidate);
                    if (!signatures.Add(signature)) { rejection = "duplicate layout signature"; continue; }
                    if (generated.Count > 0 && AdjacentOverlap(generated[generated.Count - 1], candidate) > .60f)
                    {
                        signatures.Remove(signature);
                        rejection = "adjacent layout overlap exceeded 60%";
                        continue;
                    }
                    if (generated.Count > 0 && AdjacentFeatureDifferences(
                        generated[generated.Count - 1], candidate) < 2)
                    {
                        signatures.Remove(signature);
                        rejection = "adjacent levels changed fewer than two structural features";
                        continue;
                    }
                    string physicsReason;
                    if (!GeneratedLevelPhysicsValidator.Validate(candidate, 10, out physicsReason))
                    {
                        signatures.Remove(signature);
                        rejection = physicsReason;
                        continue;
                    }
                    accepted = candidate;
                    Debug.Log("Accepted LEVEL " + playLevel.ToString("00") + " · " +
                        ArchetypeNames[playLevel - 1] + " · attempt " + (attempt + 1));
                    break;
                }
                if (accepted == null)
                    throw new InvalidOperationException("LEVEL " + playLevel.ToString("00") +
                        " could not produce a valid layout after " + MaxAttempts + " attempts: " + rejection);
                generated.Add(accepted);
            }

            const string directory = "Assets/BreakDrop/Resources/Levels";
            Directory.CreateDirectory(directory);
            foreach (LevelDefinition level in generated)
                File.WriteAllText(Path.Combine(directory, "level_" + level.levelId.ToString("000") + ".json"),
                    JsonUtility.ToJson(level, true));
            AssetDatabase.Refresh();
            Debug.Log("Generated 20 unique, physics-validated Break & Drop levels.");
        }

        private static bool TryCreateCandidate(int playLevel, int attempt, out LevelDefinition level,
            out string rejection)
        {
            Recipe recipe = RecipeFor(playLevel);
            // Skip three known edge-case packings for Level 17. Their decoy wall can settle
            // on either side of a contact tolerance even though the authored route still wins.
            int stableSeedOffset = playLevel == 17 ? 3 : 0;
            int seed = 20220908 + playLevel * 7919 + (attempt + stableSeedOffset) * 104729;
            var random = new System.Random(seed);
            HashSet<Cell> cavities = SelectCavities(recipe, random);
            HashSet<Cell> targetCells = BuildDecoyCells(recipe, cavities);
            List<PieceSpec> specs = SpecsFor(recipe.tier, random);
            List<Placement> placements;
            if (!TryPack(targetCells, specs, random, out placements))
            {
                level = null;
                rejection = "grid packing failed";
                return false;
            }
            if (!HasIndependentSupport(placements, targetCells))
            {
                level = null;
                rejection = "a decoy piece had no independent vertical support";
                return false;
            }
            level = BuildLevel(recipe, seed, placements);
            rejection = ValidateStaticRules(level, recipe);
            return rejection == null;
        }

        private static Recipe RecipeFor(int playLevel)
        {
            int tier = playLevel <= 5 ? 0 : playLevel <= 15 ? 1 : 2;
            int width = tier == 0 ? 5 : 6;
            int height = tier == 0 ? 5 : tier == 1 ? 7 : 8;
            int[] easyShafts = { 0, 3, 0, 3, 0 };
            int[] wideShafts = { 0, 1, 3, 4 };
            int shaft = tier == 0 ? easyShafts[playLevel - 1] : wideShafts[(playLevel * 3 + tier) % 4];
            return new Recipe
            {
                playLevel = playLevel, tier = tier, name = ArchetypeNames[playLevel - 1],
                width = width, height = height, shaftStart = shaft,
                blockCount = tier == 0 ? 10 : tier == 1 ? 18 : 21,
                cavityCount = tier, minimumArt = tier == 0 ? 4 : tier == 1 ? 6 : 7
            };
        }

        private static HashSet<Cell> SelectCavities(Recipe recipe, System.Random random)
        {
            var result = new HashSet<Cell>();
            if (recipe.cavityCount == 0) return result;
            int insideX = recipe.shaftStart <= 1 ? recipe.shaftStart + 2 : recipe.shaftStart - 1;
            int firstY = 1 + random.Next(Math.Max(1, recipe.height - 3));
            result.Add(new Cell(insideX, firstY));
            if (recipe.cavityCount > 1)
            {
                int secondY = Mathf.Clamp(firstY + (random.Next(2) == 0 ? 1 : 2), 1, recipe.height - 2);
                if (secondY == firstY) secondY = Mathf.Max(1, firstY - 1);
                result.Add(new Cell(insideX, secondY));
            }
            return result;
        }

        private static HashSet<Cell> BuildDecoyCells(Recipe recipe, HashSet<Cell> cavities)
        {
            var cells = new HashSet<Cell>();
            for (int y = 0; y < recipe.height; y++)
                for (int x = 0; x < recipe.width; x++)
                    if (x != recipe.shaftStart && x != recipe.shaftStart + 1)
                        cells.Add(new Cell(x, y));
            cells.ExceptWith(cavities);
            return cells;
        }

        private static List<PieceSpec> SpecsFor(int tier, System.Random random)
        {
            var specs = new List<PieceSpec>();
            if (tier == 0)
            {
                specs.Add(Spec("BlockD", "L3", 3, 3));
                specs.Add(Spec("BlockJ", "L2", 2, 3));
                specs.Add(Spec(TwoWideArt(random), "Rectangle", 2, 1));
                specs.Add(Spec("BlockG", "Rectangle", 3, 1));
                specs.Add(Spec(SquareArt(random), "Rectangle", 1, 1));
            }
            else if (tier == 1)
            {
                specs.Add(Spec("BlockD", "L3", 3, 3));
                specs.Add(Spec("BlockJ", "L2", 2, 3));
                specs.Add(Spec("BlockR", "Rectangle", 5, 1, true));
                specs.Add(Spec("BlockG", "Rectangle", 3, 1));
                specs.Add(Spec("BlockA", "Rectangle", 2, 1));
                specs.Add(Spec("BlockB", "Rectangle", 2, 1));
                specs.Add(Spec("BlockI", "Rectangle", 2, 1));
                for (int i = 0; i < 4; i++) specs.Add(Spec(SquareArt(random), "Rectangle", 1, 1));
            }
            else
            {
                specs.Add(Spec("BlockD", "L3", 3, 3));
                specs.Add(Spec("BlockD", "L3", 3, 3));
                specs.Add(Spec("BlockJ", "L2", 2, 3));
                specs.Add(Spec("BlockG", "Rectangle", 3, 1, true));
                specs.Add(Spec("BlockG", "Rectangle", 3, 1, true));
                specs.Add(Spec("BlockA", "Rectangle", 2, 1));
                specs.Add(Spec("BlockI", "Rectangle", 2, 1));
                for (int i = 0; i < 6; i++) specs.Add(Spec(SquareArt(random), "Rectangle", 1, 1));
            }
            Shuffle(specs, random);
            return specs;
        }

        private static PieceSpec Spec(string art, string shape, int width, int height, bool verticalOnly = false)
        {
            return new PieceSpec
            {
                art = art, shape = shape, width = width, height = height,
                angles = verticalOnly ? new[] { 90, 270 } : new[] { 0, 90, 270 }
            };
        }

        private static bool TryPack(HashSet<Cell> target, List<PieceSpec> specs, System.Random random,
            out List<Placement> result)
        {
            result = new List<Placement>();
            return PackRecursive(target, new HashSet<Cell>(), specs, result, random);
        }

        private static bool PackRecursive(HashSet<Cell> target, HashSet<Cell> occupied,
            List<PieceSpec> remaining, List<Placement> placed, System.Random random)
        {
            if (remaining.Count == 0)
                return occupied.SetEquals(target) && HasIndependentSupport(placed, target);
            Cell empty = target.Where(c => !occupied.Contains(c)).OrderBy(c => c.y).ThenBy(c => c.x).First();
            var candidates = new List<Tuple<int, Placement>>();
            var duplicateSpecs = new HashSet<string>();
            for (int index = 0; index < remaining.Count; index++)
            {
                PieceSpec spec = remaining[index];
                string specKey = spec.shape + ":" + spec.width + ":" + spec.height;
                if (!duplicateSpecs.Add(specKey)) continue;
                var duplicateFootprints = new HashSet<string>();
                foreach (int angle in spec.angles)
                {
                    Cell[] footprint = Footprint(spec.shape, spec.width, spec.height, angle);
                    string footprintKey = string.Join(";", footprint.Select(c => c.x + "," + c.y));
                    if (!duplicateFootprints.Add(footprintKey)) continue;
                    foreach (Cell local in footprint)
                    {
                        int ox = empty.x - local.x, oy = empty.y - local.y;
                        Cell[] cells = footprint.Select(c => new Cell(c.x + ox, c.y + oy)).ToArray();
                        if (cells.All(c => target.Contains(c) && !occupied.Contains(c)))
                            candidates.Add(Tuple.Create(index, new Placement
                            {
                                spec = spec, angle = angle, anchor = new Cell(ox, oy), cells = cells
                            }));
                    }
                }
            }
            candidates = candidates.OrderBy(_ => random.Next()).ToList();
            foreach (Tuple<int, Placement> candidate in candidates)
            {
                var nextRemaining = new List<PieceSpec>(remaining);
                nextRemaining.RemoveAt(candidate.Item1);
                foreach (Cell cell in candidate.Item2.cells) occupied.Add(cell);
                placed.Add(candidate.Item2);
                if (PackRecursive(target, occupied, nextRemaining, placed, random)) return true;
                placed.RemoveAt(placed.Count - 1);
                foreach (Cell cell in candidate.Item2.cells) occupied.Remove(cell);
            }
            return false;
        }

        private static bool HasIndependentSupport(List<Placement> placements, HashSet<Cell> decoyCells)
        {
            foreach (Placement piece in placements)
            {
                if (piece.cells.Any(c => c.y == 0)) continue;
                Cell[] contacts = piece.cells.Where(c => !piece.cells.Contains(new Cell(c.x, c.y - 1)) &&
                    decoyCells.Contains(new Cell(c.x, c.y - 1))).ToArray();
                if (contacts.Length == 0) return false;
                float center = (float)piece.cells.Average(c => c.x);
                if (center < contacts.Min(c => c.x) - .55f || center > contacts.Max(c => c.x) + .55f)
                    return false;
            }
            return true;
        }

        private static LevelDefinition BuildLevel(Recipe recipe, int seed, List<Placement> placements)
        {
            float originX = CenterX - (recipe.width - 1) * .5f;
            float supportX = originX + recipe.shaftStart + .5f;
            var blocks = new List<BlockDefinition>(recipe.blockCount);
            for (int y = 0; y < recipe.height; y++)
            {
                string art = new[] { "BlockA", "BlockB", "BlockI" }[(recipe.playLevel + y) % 3];
                blocks.Add(Block(y + 1, supportX, BaseY + y, 2, 1, "Rectangle", art, 2,
                    (seed + y) % 7 == 0 ? 360 : 0));
            }
            int nextId = recipe.height + 1;
            var decoyIds = new List<Tuple<int, Placement>>();
            foreach (Placement placement in placements)
            {
                Cell[] local = LocalOffsets(placement.spec.shape, placement.spec.width,
                    placement.spec.height, placement.angle);
                int minX2 = local.Min(c => c.x), minY2 = local.Min(c => c.y);
                float pivotX = originX + placement.anchor.x - minX2 * .5f;
                float pivotY = BaseY + placement.anchor.y - minY2 * .5f;
                float outputAngle = placement.angle == 0 && (seed + nextId) % 5 == 0 ? 360 : placement.angle;
                blocks.Add(Block(nextId, pivotX, pivotY, placement.spec.width, placement.spec.height,
                    placement.spec.shape, placement.spec.art, placement.cells.Length, outputAngle));
                decoyIds.Add(Tuple.Create(nextId, placement));
                nextId++;
            }

            var hints = new List<int>();
            if (recipe.tier == 2)
            {
                foreach (Tuple<int, Placement> decoy in decoyIds
                    .OrderByDescending(p => p.Item2.cells.Max(c => c.y)).ThenBy(p => p.Item1).Take(2))
                    hints.Add(decoy.Item1);
            }
            for (int y = recipe.height - 1; y >= 0; y--) hints.Add(y + 1);
            float targetY = BaseY + recipe.height - 1 + 1.20948206f;
            const float finalLine = -2.26923077f;
            return new LevelDefinition
            {
                levelId = recipe.playLevel + 3,
                towerLevel = true,
                bestMoves = hints.Count,
                timeLimitSeconds = recipe.tier == 0 ? 45 : recipe.tier == 1 ? 55 : 60,
                target = new VectorData { x = supportX, y = targetY },
                goal = new VectorData { x = CenterX, y = GoalY },
                goalWidth = 5.76923077f,
                failY = -5.1f,
                failX = 4.3f,
                starHeights = new[]
                {
                    Mathf.Lerp(targetY, finalLine, .33f),
                    Mathf.Lerp(targetY, finalLine, .66f),
                    finalLine
                },
                hintOrder = hints.ToArray(),
                blocks = blocks.OrderBy(b => b.id).ToArray()
            };
        }

        private static BlockDefinition Block(int id, float x, float y, float width, float height,
            string shape, string art, float mass, float rotation)
        {
            return new BlockDefinition
            {
                id = id, x = x, y = y, width = width, height = height, rotation = rotation,
                shape = shape, art = art, mass = mass, breakable = true, fixedBlock = false,
                color = ColorFor(art)
            };
        }

        private static string ValidateStaticRules(LevelDefinition level, Recipe recipe)
        {
            if (level.blocks.Length != recipe.blockCount) return "incorrect block count";
            if (level.blocks.Select(b => b.art).Distinct().Count() < recipe.minimumArt) return "insufficient art variety";
            int l3 = level.blocks.Count(b => b.shape == "L3");
            int l2 = level.blocks.Count(b => b.shape == "L2");
            int vertical = level.blocks.Count(IsVerticalBar);
            if (l3 < 1) return "missing L3";
            if (recipe.tier >= 1 && l2 < 1) return "missing L2";
            if (recipe.playLevel > 1 && vertical < 1) return "missing vertical bar";
            if (recipe.tier == 2 && (l2 + l3 < 3 || vertical < 2)) return "advanced shape mix incomplete";
            int routeMin = recipe.tier == 0 ? 4 : recipe.tier == 1 ? 7 : 10;
            int routeMax = recipe.tier == 0 ? 6 : recipe.tier == 1 ? 11 : 15;
            if (level.hintOrder.Length < routeMin || level.hintOrder.Length > routeMax)
                return "solution length outside tier range";
            if (CountInternalCavities(level) < recipe.cavityCount) return "insufficient internal cavities";
            HashSet<string> cells = OccupiedCellTokens(level);
            int expectedCells = level.blocks.Sum(b => Footprint(b.shape, Mathf.RoundToInt(b.width),
                Mathf.RoundToInt(b.height), NormalizeAngle(b.rotation)).Length);
            return cells.Count == expectedCells ? null : "overlapping occupied cells";
        }

        internal static bool IsVerticalBar(BlockDefinition block)
        {
            int angle = NormalizeAngle(block.rotation);
            return block.shape == "Rectangle" && block.width > 1 && (angle == 90 || angle == 270);
        }

        internal static string LayoutSignature(LevelDefinition level)
        {
            return string.Join("|", level.blocks.Select(b => b.shape + ":" + b.width.ToString("0") + "x" +
                b.height.ToString("0") + ":" + NormalizeAngle(b.rotation) + ":" +
                b.x.ToString("F3") + ":" + b.y.ToString("F3")).OrderBy(s => s));
        }

        internal static float AdjacentOverlap(LevelDefinition a, LevelDefinition b)
        {
            var left = new HashSet<string>(LayoutSignature(a).Split('|'));
            var right = new HashSet<string>(LayoutSignature(b).Split('|'));
            int denominator = Mathf.Min(left.Count, right.Count);
            return denominator == 0 ? 0 : left.Count(right.Contains) / (float)denominator;
        }

        internal static int AdjacentFeatureDifferences(LevelDefinition a, LevelDefinition b)
        {
            int differences = 0;
            if (OuterContourSignature(a) != OuterContourSignature(b)) differences++;
            if (!Mathf.Approximately(a.target.x, b.target.x)) differences++;
            if (PieceSetSignature(a, block => block.shape == "L2" || block.shape == "L3") !=
                PieceSetSignature(b, block => block.shape == "L2" || block.shape == "L3")) differences++;
            if (PieceSetSignature(a, IsVerticalBar) != PieceSetSignature(b, IsVerticalBar)) differences++;
            if (InternalCavitySignature(a) != InternalCavitySignature(b)) differences++;
            if (RouteSignature(a) != RouteSignature(b)) differences++;
            return differences;
        }

        private static string OuterContourSignature(LevelDefinition level)
        {
            return string.Join("|", OccupiedCellTokens(level).Select(ParseCell).GroupBy(c => c.y)
                .OrderBy(group => group.Key).Select(group =>
                    group.Key + ":" + group.Min(c => c.x) + ":" + group.Max(c => c.x)));
        }

        private static string PieceSetSignature(LevelDefinition level, Func<BlockDefinition, bool> predicate)
        {
            return string.Join("|", level.blocks.Where(predicate).Select(block => block.shape + ":" +
                NormalizeAngle(block.rotation) + ":" + block.x.ToString("F3") + ":" +
                block.y.ToString("F3")).OrderBy(value => value));
        }

        private static string RouteSignature(LevelDefinition level)
        {
            var blocks = level.blocks.ToDictionary(block => block.id);
            return string.Join("|", level.hintOrder.Select(id => blocks[id]).Select(block =>
                block.shape + ":" + NormalizeAngle(block.rotation) + ":" +
                block.x.ToString("F3") + ":" + block.y.ToString("F3")));
        }

        private static string InternalCavitySignature(LevelDefinition level)
        {
            HashSet<string> occupied = OccupiedCellTokens(level);
            Cell[] points = occupied.Select(ParseCell).ToArray();
            int minX = points.Min(c => c.x), maxX = points.Max(c => c.x);
            int minY = points.Min(c => c.y), maxY = points.Max(c => c.y);
            var cavities = new List<string>();
            for (int y = minY + 2; y <= maxY - 2; y += 2)
                for (int x = minX + 2; x <= maxX - 2; x += 2)
                    if (!occupied.Contains(x + "," + y) &&
                        occupied.Contains((x - 2) + "," + y) && occupied.Contains((x + 2) + "," + y) &&
                        occupied.Contains(x + "," + (y - 2)) && occupied.Contains(x + "," + (y + 2)))
                        cavities.Add(x + "," + y);
            return string.Join("|", cavities);
        }

        internal static int CountInternalCavities(LevelDefinition level)
        {
            HashSet<string> occupied = OccupiedCellTokens(level);
            Cell[] points = occupied.Select(ParseCell).ToArray();
            int minX = points.Min(c => c.x), maxX = points.Max(c => c.x);
            int minY = points.Min(c => c.y), maxY = points.Max(c => c.y);
            int count = 0;
            for (int y = minY + 2; y <= maxY - 2; y += 2)
                for (int x = minX + 2; x <= maxX - 2; x += 2)
                {
                    if (occupied.Contains(x + "," + y)) continue;
                    if (occupied.Contains((x - 2) + "," + y) && occupied.Contains((x + 2) + "," + y) &&
                        occupied.Contains(x + "," + (y - 2)) && occupied.Contains(x + "," + (y + 2)))
                        count++;
                }
            return count;
        }

        private static HashSet<string> OccupiedCellTokens(LevelDefinition level)
        {
            var cells = new HashSet<string>();
            foreach (BlockDefinition block in level.blocks)
            {
                Cell[] local = LocalOffsets(block.shape, Mathf.RoundToInt(block.width),
                    Mathf.RoundToInt(block.height), NormalizeAngle(block.rotation));
                int pivotX2 = Mathf.RoundToInt((block.x - CenterX) * 2);
                int pivotY2 = Mathf.RoundToInt((block.y - BaseY) * 2);
                foreach (Cell cell in local) cells.Add((pivotX2 + cell.x) + "," + (pivotY2 + cell.y));
            }
            return cells;
        }

        private static Cell ParseCell(string text)
        {
            string[] parts = text.Split(',');
            return new Cell(int.Parse(parts[0]), int.Parse(parts[1]));
        }

        private static Cell[] Footprint(string shape, int width, int height, int angle)
        {
            Cell[] doubled = LocalOffsets(shape, width, height, angle);
            int minX = doubled.Min(c => c.x), minY = doubled.Min(c => c.y);
            return doubled.Select(c => new Cell((c.x - minX) / 2, (c.y - minY) / 2)).ToArray();
        }

        private static Cell[] LocalOffsets(string shape, int width, int height, int angle)
        {
            var cells = new List<Cell>();
            if (shape == "L3" || shape == "L2")
            {
                for (int x = 0; x < width; x++)
                    cells.Add(new Cell(2 * x - (width - 1), height - 1));
                for (int y = 0; y < height - 1; y++)
                    cells.Add(new Cell(-(width - 1), 2 * y - (height - 1)));
            }
            else
            {
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        cells.Add(new Cell(2 * x - (width - 1), 2 * y - (height - 1)));
            }
            int normalized = NormalizeAngle(angle);
            return cells.Select(c => normalized == 90 ? new Cell(-c.y, c.x) :
                normalized == 270 ? new Cell(c.y, -c.x) : c).ToArray();
        }

        private static int NormalizeAngle(float angle)
        {
            int value = Mathf.RoundToInt(angle) % 360;
            return value < 0 ? value + 360 : value;
        }

        private static string TwoWideArt(System.Random random)
        {
            string[] values = { "BlockA", "BlockB", "BlockI" };
            return values[random.Next(values.Length)];
        }

        private static string SquareArt(System.Random random)
        {
            string[] values = { "BlockC", "BlockE", "BlockK", "BlockL" };
            return values[random.Next(values.Length)];
        }

        private static string ColorFor(string art)
        {
            switch (art)
            {
                case "BlockA": return "#42C6D6";
                case "BlockB": return "#E5B758";
                case "BlockC": return "#88A75B";
                case "BlockD": return "#8A70CC";
                case "BlockE": return "#E37A8E";
                case "BlockG": return "#5DAF63";
                case "BlockI": return "#C77E58";
                case "BlockJ": return "#B85B99";
                case "BlockK": return "#70B9C7";
                case "BlockL": return "#A79555";
                case "BlockR": return "#55AFC0";
                default: return "#FFFFFF";
            }
        }

        private static void Shuffle<T>(IList<T> values, System.Random random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T value = values[i];
                values[i] = values[j];
                values[j] = value;
            }
        }

        private sealed class Recipe
        {
            public int playLevel, tier, width, height, shaftStart, blockCount, cavityCount, minimumArt;
            public string name;
        }
        private sealed class PieceSpec
        {
            public string art, shape;
            public int width, height;
            public int[] angles;
        }
        private sealed class Placement
        {
            public PieceSpec spec;
            public int angle;
            public Cell anchor;
            public Cell[] cells;
        }
        private struct Cell : IEquatable<Cell>
        {
            public readonly int x, y;
            public Cell(int x, int y) { this.x = x; this.y = y; }
            public bool Equals(Cell other) { return x == other.x && y == other.y; }
            public override bool Equals(object obj) { return obj is Cell && Equals((Cell)obj); }
            public override int GetHashCode() { unchecked { return x * 397 ^ y; } }
        }
    }
}
