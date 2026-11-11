using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
public static class Art2D
{
    static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
    public static Sprite Region(string file,float x,float y,float w,float h)
    {
        string key=$"{file}:{x}:{y}:{w}:{h}";
        if(sprites.TryGetValue(key,out var sprite))return sprite;
        var texture=Resources.Load<Texture2D>("Art2D/"+file);if(texture==null)return null;
        sprite=Sprite.Create(texture,new Rect(x*texture.width,(1-y-h)*texture.height,w*texture.width,h*texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
        sprites[key]=sprite;return sprite;
    }
    public static Image Picture(Sprite sprite,int width,int height)
    {
        var image=new Image{sprite=sprite,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};image.style.width=width;image.style.height=height;image.style.flexShrink=0;return image;
    }
    public static Sprite Portrait(int index)=>Region("source-05",index/6f,.18f,1f/6f,.27f);
    public static Sprite Icon(int index)=>Region("source-06",(index%4)/4f,(index/4)/4f,.25f,.25f);
}
