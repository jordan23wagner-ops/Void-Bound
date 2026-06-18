using UnityEngine;
using VoidBound.Combat;
using VoidBound.Core;
using VoidBound.Data;

namespace VoidBound.Skilling
{
    public class ResourceNode : Interactable
    {
        [SerializeField] private MaterialItemSO gatherMaterial;
        [SerializeField] private int gatherQuantity = 1;
        [SerializeField] private SkillType gatherSkill;
        [SerializeField] private int xpPerGather = 15;
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
            if (depleted || gatherMaterial == null) return;

            var matInv = instigator.GetComponent<MaterialInventory>();
            var skills = instigator.GetComponent<PlayerSkills>();
            if (matInv == null) return;

            matInv.AddMaterial(gatherMaterial, gatherQuantity);
            skills?.AddXP(gatherSkill, xpPerGather);

            FloatingDamageNumber.SpawnText(transform.position,
                $"+{gatherQuantity} {gatherMaterial.displayName}", new Color(0.4f, 0.9f, 0.3f));

            depleted = true;
            depletedTimer = respawnTime;
            if (nodeRenderer != null)
                nodeRenderer.enabled = false;
        }
    }
}
