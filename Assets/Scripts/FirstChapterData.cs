using System;
using UnityEngine;

namespace Jiaozi
{
    [Serializable]
    public sealed class ArtisanDefinition
    {
        public string id, displayName;
        public Color color;
        public GameObject visualPrefab;
        public Vector3 handOffset = new Vector3(0, .85f, .5f);
        public RuntimeAnimatorController animator;
    }

    [CreateAssetMenu(menuName = "Jiaozi/First Chapter")]
    public sealed class FirstChapterData : ScriptableObject
    {
        public string levelId = "first-order", levelName = "开张第一单";
        public WorkshopConfig rules = new WorkshopConfig();
        public int[] starOrders =
        {
            2,
            4,
            6
        };
        public Vector3[] spawnPoints =
        {
            new Vector3(-4, 0, -.7f),
            new Vector3(3, 0, -.7f)
        };
        public Graybox.StationLayout[] stations;
        public Vector3[] partitions;
        public ArtisanDefinition[] artisans =
        {
            new ArtisanDefinition
            {
                id = "zhituan",
                displayName = "纸团",
                color = new Color(.1f, .62f, .67f)
            },
            new ArtisanDefinition
            {
                id = "yintao",
                displayName = "印桃",
                color = new Color(.87f, .38f, .2f)
            }
        };
        public Texture2D ticketPaper, ticketInk, ticketSeal;
        public string PairName(int a, int b) => artisans[a].displayName + " ＋ " + artisans[b].displayName;
    }
}
