using NUnit.Framework;
using UnityEngine;
using Jiaozi;
public class DisplayPolicyTests
{
    [TestCase(0)][TestCase(1)][TestCase(2)]
    public void FullscreenAlwaysUsesDesktopPixels(int preset){Assert.AreEqual(new Vector2Int(2560,1600),DisplayPolicy.Resolution(new GameSettings{fullscreen=true,resolution=preset},2560,1600));}
    [Test]public void WindowRetainsSelectedPreset(){Assert.AreEqual(new Vector2Int(1920,1080),DisplayPolicy.Resolution(new GameSettings{fullscreen=false,resolution=1},2560,1600));}
    [Test]public void MissingDesktopFallsBackToValidPreset(){Assert.AreEqual(new Vector2Int(1280,800),DisplayPolicy.Resolution(new GameSettings{fullscreen=true},0,0));}
}
