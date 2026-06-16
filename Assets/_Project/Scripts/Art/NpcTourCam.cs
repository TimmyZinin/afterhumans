using System.Collections;
using UnityEngine;

namespace Afterhumans.Art
{
    /// <summary>
    /// QA ACCEPTANCE TOUR — no effect in normal play. Append <c>?tour=1</c> to the WebGL URL
    /// and the MAIN camera flies to each NPC's face under the REAL build lighting (each NPC is
    /// turned to face the camera so the head/face is unmistakable), then drops the dog next to
    /// the last NPC so the proximity dialogue HUD appears on screen. This lets the NPC acceptance
    /// agent judge the REAL game (heads / lighting / scale / dialogue window) instead of editor
    /// fake-light renders. Self-destructs unless tour=1 is present.
    /// </summary>
    public class NpcTourCam : MonoBehaviour
    {
        public float perNpc = 3.5f;
        public float dist = 1.8f;
        public float headH = 1.5f;

        private static readonly string[] Ids = { "sasha", "mila", "kirill", "nikolai", "stas" };

        private void Start()
        {
            var url = Application.absoluteURL ?? "";
            if (!url.Contains("tour=1")) { Destroy(this); return; }
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return new WaitForSeconds(1.5f);
            var cam = Camera.main;
            if (cam == null) { Debug.Log("[TOUR] no main camera"); yield break; }

            // Stop anything that would fight the camera or the NPC pose during the tour.
            // (NpcVoice is left ON so the final HUD phase can trigger the dialogue window.)
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                var n = mb.GetType().Name;
                if (n == "KafkaDirectController" || n == "NpcIdleBob" || n == "NpcWalk") mb.enabled = false;
            }

            float baseFov = cam.fieldOfView;
            cam.fieldOfView = 38f;   // tighter for a face-level closeup

            GameObject last = null;
            foreach (var id in Ids)
            {
                var npc = GameObject.Find("NPC_" + id);
                if (npc == null) { Debug.Log("[TOUR] missing " + id); continue; }
                last = npc;

                // Aim at the NPC's ACTUAL head from renderer bounds — transform.position.y already
                // includes sit/ground height, so a fixed +1.5 offset overshot above the head into
                // the glass/ceiling. Use bounds top instead.
                var rends = npc.GetComponentsInChildren<Renderer>();
                if (rends.Length == 0) { Debug.Log("[TOUR] no renderer " + id); continue; }
                Bounds b = rends[0].bounds; foreach (var r in rends) b.Encapsulate(r.bounds);
                Vector3 head = new Vector3(b.center.x, b.max.y - b.size.y * 0.12f, b.center.z);
                float reach = Mathf.Max(b.size.y * 0.9f, 1.1f);

                // Camera on the GLASS/perimeter side (away from room centre) looking inward, so the
                // NPC (turned to face the camera) faces the sun → front-lit, with the darker room
                // interior behind it instead of a blown-out window.
                Vector3 dir = npc.transform.position; dir.y = 0f;
                dir = (dir.sqrMagnitude < 0.01f) ? Vector3.forward : dir.normalized;
                Vector3 camPos = head + dir * reach;
                cam.transform.position = camPos;
                cam.transform.rotation = Quaternion.LookRotation(head - camPos, Vector3.up);

                // turn the NPC to face the camera so its head/face is unmistakable
                Vector3 face = camPos - npc.transform.position; face.y = 0f;
                if (face.sqrMagnitude > 0.01f) npc.transform.rotation = Quaternion.LookRotation(face, Vector3.up);

                Debug.Log("[TOUR] show " + id + " headY=" + head.y.ToString("F2") + " reach=" + reach.ToString("F2"));
                yield return new WaitForSeconds(perNpc);
            }
            cam.fieldOfView = baseFov;

            // HUD phase: drop the dog next to the last framed NPC so the proximity dialogue
            // window (NpcDialogueHud) appears on screen — proves «собака подошла → окно диалога».
            if (last != null)
            {
                var dog = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Hero_Corgi");
                if (dog != null)
                {
                    Vector3 d = last.transform.position; d.y = 0f;
                    d = (d.sqrMagnitude < 0.01f) ? Vector3.back : -d.normalized;
                    Vector3 p = last.transform.position + d * 1.6f; p.y = dog.transform.position.y;
                    var cc = dog.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;   // allow a direct teleport
                    dog.transform.position = p;
                    if (cc != null) cc.enabled = true;
                    Debug.Log("[TOUR] hud (dog @ " + last.name + ")");
                    yield return new WaitForSeconds(4f);
                }
            }
            Debug.Log("[TOUR] done");
        }
    }
}
