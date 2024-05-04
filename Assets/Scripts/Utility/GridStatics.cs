using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleDrakeCreations.TTBTk
{
    public static class GridStatics
    {
        public static Vector3 SnapVectorToVector(Vector3 vectorToSnap, Vector3 snapLocation)
        {
            return new Vector3(
                Mathf.Round(vectorToSnap.x / snapLocation.x) * snapLocation.x,
                Mathf.Round(vectorToSnap.y / snapLocation.y) * snapLocation.y,
                Mathf.Round(vectorToSnap.z / snapLocation.z) * snapLocation.z
                );
        }

        public static bool IsFloatEven(float value)
        {
            return value % 2 == 0;
        }

        public static GridShapeData GetGridShapeData(GridShape gridShape)
        {
            GridShapeData[] _gridshapeData = Resources.LoadAll<GridShapeData>("Data/Grid");
            return _gridshapeData.FirstOrDefault<GridShapeData>(data => data.gridShape == gridShape);
        }

        public static bool IsTileTypeWalkable(TileType tileType)
        {
            return tileType == TileType.Normal;
        }
    }
}