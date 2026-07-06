using UnityEngine;
using VoidBound.Combat;
using VoidBound.Core;
using VoidBound.Data;

namespace VoidBound.Skilling
{
    public class ResourceNode : Interactable
    {
        [SerializeField] private MaterialItemSO gatherMaterial;
        // Optional tool-gated yield: index = resource rank. The player's tool
        // tier for gatherSkill (PlayerTools) caps which ranks can drop; a catch
        // is random among the unlocked ranks. Falls back to gatherMaterial.
        [SerializeField] private MaterialItemSO[] tieredMaterials;
        [SerializeField] private int gatherQuantity = 1;
        [SerializeField] private SkillType gatherSkill;
        [SerializeField] private int xpPerGather = 15; // legacy; gather skills no longer level
        [SerializeField] private float respawnTime = 10f;

        private bool depleted;
        private float depletedTimer;
        private Renderer nodeRenderer;

        private void Awake()
        {
            nodeRenderer = GetComponentInChildren<Renderer>();
        }

        private void Update()
        {
            if (!depleted) return;

            depletedTimer -= Time.deltaTime;
            if (depletedTimer <= 0f)
            {
                depleted = false;
                if (nodeRenderer != null)
                    nodeRenderer.enabled = true;
            }
        }

        public override void OnInteract(GameObject instigator)
        {
            if (depleted) return;

            var matInv = instigator.GetComponent<MaterialInventory>();
            if (matInv == null) return;

            var mat = PickMaterial(instigator);
            if (mat == null) return;

            matInv.AddMaterial(mat, gatherQuantity);
            FloatingDamageNumber.SpawnText(transform.position,
                $"+{gatherQuantity} {mat.displayName}", new Color(0.4f, 0.9f, 0.3f));

            depleted = true;
            depletedTimer = respawnTime;
            if (nodeRenderer != null)
                nodeRenderer.enabled = false;
        }

        // Tool-gated ranks if configured, else the single gatherMaterial.
        private MaterialItemSO PickMaterial(GameObject instigator)
        {
            if (tieredMaterials != null && tieredMaterials.Length > 0)
            {
                var tools = instigator.GetComponent<PlayerTools>();
                int tier = tools != null ? (int)tools.GetToolTier(gatherSkill) : 0;
                int maxRank = Mathf.Clamp(tier, 0, tieredMaterials.Length - 1);
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    var pick = tieredMaterials[Random.Range(0, maxRank + 1)];
                    if (pick != null) return pick;
                }
            }
            return gatherMaterial;
        }
    }
}
