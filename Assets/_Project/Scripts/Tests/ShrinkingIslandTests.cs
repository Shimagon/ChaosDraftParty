using ChaosDraftParty.Minigames.ShrinkingIsland;
using NUnit.Framework;
using UnityEngine;

namespace ChaosDraftParty.Tests
{
    public class ShrinkingIslandTests
    {
        [Test]
        public void Tile_StartFade_EntersWarning_AndResetRestoresHealthy()
        {
            GameObject tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                Tile tile = tileObject.AddComponent<Tile>();
                tile.Initialize(0, 0, 0);

                tile.StartFade(1f, 0.5f);

                Assert.That(tile.State, Is.EqualTo(Tile.TileState.Warning));

                tile.ResetTile();

                Assert.That(tile.State, Is.EqualTo(Tile.TileState.Healthy));
                Assert.That(tileObject.activeSelf, Is.True);
                Assert.That(tileObject.GetComponent<MeshRenderer>().enabled, Is.True);
                Assert.That(tileObject.GetComponent<Collider>().enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(tileObject);
            }
        }
    }
}
