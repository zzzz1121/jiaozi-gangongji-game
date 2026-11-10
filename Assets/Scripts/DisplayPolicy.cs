using UnityEngine;
namespace Jiaozi
{
    public static class DisplayPolicy
    {
        public static Vector2Int Resolution(GameSettings settings,int desktopWidth,int desktopHeight)
        {
            if(settings.fullscreen&&desktopWidth>0&&desktopHeight>0)return new Vector2Int(desktopWidth,desktopHeight);
            int index=Mathf.Clamp(settings.resolution,0,2);
            return new Vector2Int(new[]{1280,1920,1280}[index],new[]{800,1080,720}[index]);
        }
    }
}
