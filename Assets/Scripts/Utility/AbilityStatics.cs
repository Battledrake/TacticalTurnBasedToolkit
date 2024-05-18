using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleDrakeCreations.TacticalTurnBasedTemplate
{
    public static class AbilityStatics
    {
        public static bool HasLineOfSight(TileData origin, TileData target, float height, float distance)
        {
            Vector3 startPosition = origin.tileMatrix.GetPosition();
            startPosition.y += height;
            Vector3 targetPosition = target.tileMatrix.GetPosition();
            targetPosition.y += height;
            Vector3 targetDirection = targetPosition - startPosition;

            if (Physics.Raycast(startPosition, targetDirection, out RaycastHit hitInfo, distance))
            {
                return true;
            }
            return false;
        }

        public static List<GridIndex> GetIndexesFromPatternAndRange(GridIndex origin, GridShape gridShape, Vector2Int rangeMinMax, AbilityRangePattern pattern)
        {
            List<GridIndex> patternList = new List<GridIndex>();
            switch (pattern)
            {
                case AbilityRangePattern.None:
                    patternList.Add(origin);
                    break;
                case AbilityRangePattern.Line:
                    patternList = GetLinePattern(origin, gridShape, rangeMinMax);
                    break;
                case AbilityRangePattern.Diagonal:
                    patternList = GetDiagonalPattern(origin, gridShape, rangeMinMax);
                    break;
                case AbilityRangePattern.HalfDiagonal:
                    patternList = GetHalfDiagonalPattern(origin, gridShape, rangeMinMax);
                    break;
                case AbilityRangePattern.Star:
                    patternList = GetStarPattern(origin, gridShape, rangeMinMax);
                    break;
                case AbilityRangePattern.Diamond:
                    patternList = GetDiamondPattern(origin, gridShape, rangeMinMax);
                    break;
                case AbilityRangePattern.Square:
                    patternList = GetSquarePattern(origin, gridShape, rangeMinMax);
                    break;
            }
            return OffsetIndexArray(patternList, origin);
        }

        private static List<GridIndex> OffsetIndexArray(List<GridIndex> indexList, GridIndex offset)
        {
            List<GridIndex> returnList = new List<GridIndex>();
            indexList.ForEach(i => returnList.Add(i + offset));
            return returnList;
        }

        private static List<GridIndex> GetLinePattern(GridIndex origin, GridShape shape, Vector2Int rangeMinMax)
        {
            List<GridIndex> returnList = new List<GridIndex>();
            for (int i = rangeMinMax.x; i <= rangeMinMax.y; i++)
            {
                switch (shape)
                {
                    case GridShape.Square:
                        returnList.Add(new GridIndex(i, 0));
                        returnList.Add(new GridIndex(-i, 0));
                        returnList.Add(new GridIndex(0, i));
                        returnList.Add(new GridIndex(0, -i));
                        break;

                    case GridShape.Hexagon:
                        returnList.Add(new GridIndex(-i, 0));
                        returnList.Add(new GridIndex(i, 0));

                        int negX = origin.z % 2 == 0 ? Mathf.FloorToInt(-i / 2f) : Mathf.CeilToInt(-i / 2f);
                        int posX = origin.z % 2 == 0 ? Mathf.FloorToInt(i / 2f) : Mathf.CeilToInt(i / 2f);

                        returnList.Add(new GridIndex(posX, i));
                        returnList.Add(new GridIndex(negX, i));
                        returnList.Add(new GridIndex(posX, -i));
                        returnList.Add(new GridIndex(negX, -i));

                        break;

                    case GridShape.Triangle:
                        returnList.Add(new GridIndex(i, 0));
                        returnList.Add(new GridIndex(-i, 0));

                        if (GridStatics.IsTriangleTileFacingUp(origin))
                        {
                            returnList.Add(new GridIndex(Mathf.CeilToInt(i * 0.5f), Mathf.FloorToInt(i * 0.5f)));
                            returnList.Add(new GridIndex(-Mathf.CeilToInt(i * 0.5f), Mathf.FloorToInt(i * 0.5f)));
                            returnList.Add(new GridIndex(Mathf.CeilToInt(i * -0.5f), Mathf.FloorToInt(i * -0.5f)));
                            returnList.Add(new GridIndex(-Mathf.CeilToInt(i * -0.5f), Mathf.FloorToInt(i * -0.5f)));
                        }
                        else
                        {
                            returnList.Add(new GridIndex(Mathf.FloorToInt(i * 0.5f), Mathf.CeilToInt(i * 0.5f)));
                            returnList.Add(new GridIndex(-Mathf.FloorToInt(i * 0.5f), Mathf.CeilToInt(i * 0.5f)));
                            returnList.Add(new GridIndex(Mathf.FloorToInt(i * -0.5f), Mathf.CeilToInt(i * -0.5f)));
                            returnList.Add(new GridIndex(-Mathf.FloorToInt(i * -0.5f), Mathf.CeilToInt(i * -0.5f)));
                        }
                        break;
                }
            }
            return returnList;
        }

        private static List<GridIndex> GetDiagonalPattern(GridIndex origin, GridShape gridShape, Vector2Int rangeMinMax)
        {
            List<GridIndex> returnList = new List<GridIndex>();
            for (int i = rangeMinMax.x; i <= rangeMinMax.y; i++)
            {
                switch (gridShape)
                {

                    case GridShape.Square:
                        returnList.Add(new GridIndex(i, i));
                        returnList.Add(new GridIndex(-i, -i));
                        returnList.Add(new GridIndex(-i, i));
                        returnList.Add(new GridIndex(i, -i));
                        break;

                    case GridShape.Hexagon:
                        returnList.Add(new GridIndex(0, i * 2)); //up
                        returnList.Add(new GridIndex(0, -i * 2)); // down
                        returnList.Add(new GridIndex(Mathf.CeilToInt(i * 3), i)); //up right
                        returnList.Add(new GridIndex(-Mathf.CeilToInt(i * 3), i)); //up left
                        returnList.Add(new GridIndex(Mathf.CeilToInt(i * 3), -i)); //Down Right
                        returnList.Add(new GridIndex(-Mathf.CeilToInt(i * 3), -i)); //Down Left
                        break;

                    case GridShape.Triangle:
                        returnList.Add(new GridIndex(0, i));
                        returnList.Add(new GridIndex(0, -i));

                        if (GridStatics.IsTriangleTileFacingUp(origin))
                        {
                            returnList.Add(new GridIndex(Mathf.FloorToInt(i * 0.5f * 3.0f), Mathf.FloorToInt(i * 0.5f)));
                            returnList.Add(new GridIndex(-Mathf.FloorToInt(i * 0.5f * 3.0f), Mathf.FloorToInt(i * 0.5f)));
                            returnList.Add(new GridIndex(Mathf.FloorToInt(i * -0.5f * 3.0f), Mathf.FloorToInt(i * -0.5f)));
                            returnList.Add(new GridIndex(-Mathf.FloorToInt(i * -0.5f * 3.0f), Mathf.FloorToInt(i * -0.5f)));
                        }
                        else
                        {
                            returnList.Add(new GridIndex(Mathf.CeilToInt(i * 0.5f * 3.0f), Mathf.CeilToInt(i * 0.5f)));
                            returnList.Add(new GridIndex(-Mathf.CeilToInt(i * 0.5f * 3.0f), Mathf.CeilToInt(i * 0.5f)));
                            returnList.Add(new GridIndex(Mathf.CeilToInt(i * -0.5f * 3.0f), Mathf.CeilToInt(i * -0.5f)));
                            returnList.Add(new GridIndex(-Mathf.CeilToInt(i * -0.5f * 3.0f), Mathf.CeilToInt(i * -.5f)));
                        }
                        break;
                }
            }
            return returnList;
        }

        public static List<GridIndex> GetHalfDiagonalPattern(GridIndex origin, GridShape gridShape, Vector2Int rangeMinMax)
        {
            return GetDiagonalPattern(origin, gridShape, rangeMinMax / 2);
        }

        public static List<GridIndex> GetStarPattern(GridIndex origin, GridShape gridShape, Vector2Int rangeMinMax)
        {
            List<GridIndex> returnList = GetLinePattern(origin, gridShape, rangeMinMax);

            if (gridShape == GridShape.Square)
            {
                GetDiagonalPattern(origin, gridShape, rangeMinMax).ForEach(i => returnList.Add(i));
                return returnList;
            }
            else
            {
                GetHalfDiagonalPattern(origin, gridShape, rangeMinMax).ForEach(i => returnList.Add(i));
                return returnList;
            }
        }

        private static List<GridIndex> GetSquarePattern(GridIndex origin, GridShape gridShape, Vector2Int rangeMinMax)
        {
            List<GridIndex> returnList = new List<GridIndex>();
            for (int i = rangeMinMax.x; i <= rangeMinMax.y; i++)
            {
                switch (gridShape)
                {
                    case GridShape.Square:
                        for (int j = -i; j <= i; j++)
                        {
                            returnList.Add(new GridIndex(-i, j));
                            returnList.Add(new GridIndex(j, i));
                            returnList.Add(new GridIndex(i, -j));
                            returnList.Add(new GridIndex(-j, -i));
                        }
                        break;
                    default:
                        for (int j = -i; j <= i; j++)
                        {
                            returnList.Add(new GridIndex(-i * 2, j)); //Down To up, Left
                            returnList.Add(new GridIndex(i * 2, -j)); //Up to down, right
                            if (i != j)
                            {
                                returnList.Add(new GridIndex(-i * 2 + 1, j)); //down to up, left
                                returnList.Add(new GridIndex(i * 2 - 1, -j)); //up to down, right
                            }
                        }
                        for (int j = -i * 2; j <= i * 2; j++)
                        {
                            returnList.Add(new GridIndex(j, i)); //Up, Left to Right
                            returnList.Add(new GridIndex(-j, -i)); //Down, right to left
                        }
                        break;
                }
            }
            return returnList;
        }

        private static List<GridIndex> GetDiamondPattern(GridIndex origin, GridShape gridShape, Vector2Int rangeMinMax)
        {
            List<GridIndex> returnList = new List<GridIndex>();

            for (int i = rangeMinMax.x; i <= rangeMinMax.y; i++)
            {
                switch (gridShape)
                {
                    case GridShape.Square:
                        for (int j = 0; j <= i; j++)
                        {
                            returnList.Add(new GridIndex(-(i - j), j));
                            returnList.Add(new GridIndex(j, i - j));
                            returnList.Add(new GridIndex(i - j, -j));
                            returnList.Add(new GridIndex(-j, -(i - j)));
                        }
                        break;
                    case GridShape.Hexagon:
                        for (int j = 0; j <= 0; j++)
                        {
                            returnList.Add(new GridIndex(-(i * 2 - j), j)); //Mid to up, left to right
                            returnList.Add(new GridIndex(i * 2 - j, -j)); //Mid to down, right to left
                            returnList.Add(new GridIndex(i * 2 - (i - j), i - j)); //Up to Mid, left to right
                            returnList.Add(new GridIndex(-i * 2 - (i - j), -(i - j))); //Down to mid. Right to left.
                        }
                        for (int j = -i - 2; j <= i - 2; j++)
                        {
                            returnList.Add(new GridIndex(j, i)); //up, midleft to midright
                            returnList.Add(new GridIndex(-j, -i)); //up midleft to midright
                        }
                        break;
                    case GridShape.Triangle:
                        bool isFacingUp = GridStatics.IsTriangleTileFacingUp(origin);
                        for (int j = 0; j <= i; j++)
                        {
                            int z = isFacingUp ? j : -j;
                            int x = i * 2 - j;

                            returnList.Add(new GridIndex(-x, z));
                            returnList.Add(new GridIndex(x, -z));

                            if (j != i)
                                returnList.Add(new GridIndex(-x + 1, z));

                            if (j != 0)
                                returnList.Add(new GridIndex(x + 1, -z));


                            z = isFacingUp ? i - j : -(i - j);
                            x = (i * 2) - (i - j);

                            returnList.Add(new GridIndex(-x, -z));
                            returnList.Add(new GridIndex(x, z));

                            if (j != i)
                                returnList.Add(new GridIndex(-x - 1, -z));

                            if (j != 0)
                                returnList.Add(new GridIndex(x - 1, z));

                        }
                        for (int j = -i; j <= i; j++)
                        {
                            returnList.Add(new GridIndex(j, isFacingUp ? i : -i));
                            returnList.Add(new GridIndex(-j, isFacingUp ? -i : i));
                        }
                        break;
                }
            }
            return returnList;
        }
    }
}