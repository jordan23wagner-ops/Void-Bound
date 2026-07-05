using UnityEngine;
using VoidBound.Combat;

namespace VoidBound.NPC
{
    // Ambient town villager: strolls to random points within a radius of its
    // home, pausing between walks, driving the shared skeletal Walk/Idle via
    // CharacterAnimation. An "attend" villager instead stands at its post facing
    // a fixed direction (e.g. a shopkeeper's customer at the merchant stall).
    // No pathfinding — the Homestead is open, so simple steering suffices.
    [RequireComponent(typeof(CharacterController))]
    public class VillagerWander : MonoBehaviour
    {
        [SerializeField] private float radius = 4f;
        [SerializeField] private float speed = 1.3f;
        [SerializeField] private bool attend = false;
        [SerializeField] private Vector3 faceDir = Vector3.forward;
        [SerializeField] private float minPause = 1.5f;
        [SerializeField] private float maxPause = 4.5f;

        private Vector3 home;
        private Vector3 dest;
        private CharacterController cc;
        private CharacterAnimation anim;
        private float waitUntil;
        private float verticalVel;
        private bool waiting;

        private void Start()
        {
            cc = GetComponent<CharacterController>();
            anim = GetComponent<CharacterAnimation>();
            home = transform.position;

            if (attend)
            {
                FaceDir(faceDir);
                if (anim != null) anim.SetSpeed(0f);
            }
            else
            {
                PickDest();
            }
        }

        private void Update()
        {
            Vector3 move = Vector3.zero;

            if (!attend)
            {
                if (waiting)
                {
                    if (anim != null) anim.SetSpeed(0f);
                    if (Time.time >= waitUntil) { PickDest(); waiting = false; }
                }
                else
                {
                    Vector3 to = dest - transform.position; to.y = 0f;
                    if (to.sqrMagnitude < 0.2f)
                    {
                        waiting = true;
                        waitUntil = Time.time + Random.Range(minPause, maxPause);
                        if (anim != null) anim.SetSpeed(0f);
                    }
                    else
                    {
                        Vector3 dir = to.normalized;
                        transform.rotation = Quaternion.Slerp(transform.rotation,
                            Quaternion.LookRotation(dir), 8f * Time.deltaTime);
                        move = dir * speed;
                        if (anim != null) anim.SetSpeed(1f);
                    }
                }
            }
            else if (anim != null)
            {
                anim.SetSpeed(0f);
            }

            // Gravity so the controller stays grounded on the terrain.
            if (cc.isGrounded && verticalVel < 0f) verticalVel = -2f;
            else verticalVel += -20f * Time.deltaTime;
            move.y = verticalVel;
            cc.Move(move * Time.deltaTime);
        }

        private void PickDest()
        {
            Vector2 c = Random.insideUnitCircle * radius;
            dest = home + new Vector3(c.x, 0f, c.y);
        }

        private void FaceDir(Vector3 d)
        {
            d.y = 0f;
            if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d.normalized);
        }
    }
}
