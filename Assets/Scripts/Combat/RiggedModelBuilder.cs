using UnityEngine;

namespace VoidBound.Combat
{
    // Runtime equivalent of the editor CharacterModelSwap: dresses an enemy/NPC root
    // in a rigged model as a "Model" child (SkinnedMeshRenderer + Animator), with the
    // -Z-facing FBX turned 180° so it faces the root's forward. The root keeps the
    // CharacterController + gameplay scripts; the child is purely visual. Used by
    // EnemySpawner so spawned foes look as polished as the hand-placed / boss models.
    public static class RiggedModelBuilder
    {
        // slotMaterials is the per-submesh material set, ordered to match the FBX's
        // material slots (skin + baked-in gear: Cloth/Dark/Gold/Gem/Bone), mapped at
        // setup time so the sculpted armour renders in its proper colours instead of
        // being flattened to one skin tint.
        public static void Attach(GameObject root, GameObject fbx, RuntimeAnimatorController controller,
            Material[] slotMaterials, float scale)
        {
            if (root == null || fbx == null) return;

            // Strip any placeholder mesh/model already on the root.
            var mf = root.GetComponent<MeshFilter>();   if (mf != null) Object.Destroy(mf);
            var mr = root.GetComponent<MeshRenderer>();  if (mr != null) Object.Destroy(mr);
            var old = root.transform.Find("Model");      if (old != null) Object.Destroy(old.gameObject);

            var model = Object.Instantiate(fbx);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            model.transform.localScale = Vector3.one * (scale <= 0f ? 1f : scale);

            var anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;

            var smr = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && slotMaterials != null && slotMaterials.Length > 0)
            {
                int n = smr.sharedMaterials.Length;
                var mats = new Material[n];
                for (int i = 0; i < n; i++)
                    mats[i] = slotMaterials[Mathf.Min(i, slotMaterials.Length - 1)];
                smr.sharedMaterials = mats;
            }

            // CharacterAnimation drives Speed/Attack/Hit/Dead; EnemyAI reads it.
            if (root.GetComponent<CharacterAnimation>() == null)
                root.AddComponent<CharacterAnimation>();
        }
    }
}
