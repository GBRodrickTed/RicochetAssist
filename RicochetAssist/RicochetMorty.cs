using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;

namespace RicochetAssist
{
    public static class RicochetMorty
    {
        static int ricochetCount = 0;
        static int damCount = 0;
        public static float ricFOV = 180;
        public static float ricTimer = 0.1f;
        public static bool shouldAimAssist = true;
        public static bool shouldVanillaAimAssist = false;
        public static bool shouldRailBounce = false;
        public static bool shouldTargetCoin = true;
        public static int railBounceAmount = 5;
        public static int ricBounceAmount = 5;
        static bool eidCheck(EnemyIdentifier eid) //never used :sob emoticon:
        {
            return (eid != null && !eid.dead && (eid.gameObject) && !(eid.blessed));
        }
        
        [HarmonyPatch(typeof(RevolverBeam), nameof(RevolverBeam.RicochetAimAssist))]
        [HarmonyPrefix]
        public static bool TargetedHarassmentCampaign(RevolverBeam __instance)
        {
            if (!shouldAimAssist) return shouldVanillaAimAssist;
            float minDist = float.PositiveInfinity;
            GameObject mainObject = null;
            Transform target = null;
            EnemyIdentifier eid = null;
            RevolverBeam revb = __instance;
            if (CoinList.Instance.revolverCoinsList.Count > 0 && shouldTargetCoin)
            {
                foreach (Coin coin in CoinList.Instance.revolverCoinsList)
                {
                    if (coin != null && (!coin.shot || coin.shotByEnemy))
                    {
                        Vector3 thing2fromthing1 = (coin.transform.position - revb.transform.position);
                        if (!Utils.WithinFOV(revb.transform.forward, (thing2fromthing1), ricFOV))
                        {
                            continue;
                        }
                        float dist = thing2fromthing1.sqrMagnitude;
                        if (dist < minDist)
                        {
                            RaycastHit rayHit;
                            if (!Physics.Raycast(revb.transform.position, thing2fromthing1, out rayHit, Vector3.Distance(revb.transform.position, coin.transform.position) - 0.5f, coin.lmask))
                            {
                                mainObject = coin.gameObject;
                                minDist = dist;
                            }
                        }
                    }
                }
            }
            if (mainObject == null)
            {
                minDist = float.PositiveInfinity;
                foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
                {
                    Vector3 thing2fromthing1 = enemy.transform.position - revb.transform.position;
                    float dist = (thing2fromthing1).sqrMagnitude;
                    if (dist < minDist)
                    {
                        if (!Utils.WithinFOV(revb.transform.forward, (thing2fromthing1), ricFOV))
                        {
                            continue;
                        }//
                        eid = enemy.GetComponent<EnemyIdentifier>();
                        if (eid != null && !eid.dead && (eid.gameObject) && !(eid.blessed))
                        {
                            if (eid.weakPoint != null && eid.weakPoint.activeInHierarchy)
                            {
                                target = eid.weakPoint.transform;
                            }
                            else
                            {
                                EnemyIdentifierIdentifier eidid = eid.GetComponentInChildren<EnemyIdentifierIdentifier>();
                                if (eidid && eidid.eid && eidid.eid == eid)
                                {
                                    target = eidid.transform;
                                }
                                else
                                {
                                    target = eid.transform;
                                }
                            }
                            RaycastHit rayHit;
                            if (!Physics.Raycast(revb.transform.position, target.position - revb.transform.position, out rayHit, Vector3.Distance(revb.transform.position, target.position) - 0.5f, LayerMaskDefaults.Get(LMD.Environment)))
                            {
                                mainObject = target.gameObject;
                                minDist = dist;
                            }
                            else
                            {
                                eid = null;
                            }
                        }
                        else
                        {
                            eid = null;
                        }
                    }
                }
            }
            if (mainObject != null)
            {
                revb.gameObject.transform.LookAt(mainObject.transform.position);
            }
            return shouldVanillaAimAssist;
        }
        [HarmonyPatch(typeof(RevolverBeam), nameof(RevolverBeam.Start))]
        [HarmonyPrefix]
        public static bool RicochetForAll(RevolverBeam __instance)
        {
            if (__instance != null && !__instance.aimAssist) // The first shot doesn't have aim assist
            {
                if (__instance.beamType == BeamType.Railgun && shouldRailBounce)
                {
                    __instance.ricochetAmount += railBounceAmount;
                    return true;
                }
                if (__instance.canHitProjectiles)
                {
                    __instance.ricochetAmount += ricBounceAmount;
                    return true;
                }
            }
            return true;
        }
        [HarmonyPatch(typeof(RevolverBeam), nameof(RevolverBeam.PiercingShotCheck))]
        [HarmonyPrefix]
        public static void RicochetPreCheck(RevolverBeam __instance)
        {
            //ricochetCount = __instance.ricochetAmount;
            if (DelayedActivationManager.instance != null)
            {
                damCount = DelayedActivationManager.instance.toActivate.Count;
            }
        }
        [HarmonyPatch(typeof(RevolverBeam), nameof(RevolverBeam.PiercingShotCheck))]
        [HarmonyPostfix]
        public static void RicochetPostCheck(RevolverBeam __instance)
        {
            if (DelayedActivationManager.instance != null && damCount < DelayedActivationManager.instance.toActivate.Count) // lazior has been added
            {
                DelayedActivationManager dam = DelayedActivationManager.instance;
                RevolverBeam revb;
                for (int i = dam.toActivate.Count-1; i >= 0; i--)
                {
                    if (dam.toActivate[i].TryGetComponent(out revb))
                    {
                        if (revb.ricochetAmount == __instance.ricochetAmount)
                        {
                            /*dam.toActivate.RemoveAt(i);
                            dam.activateCountdowns.RemoveAt(i);
                            break;*/
                            //revb.ricochetAmount += 100;
                            dam.activateCountdowns[i] = ricTimer;
                            break;
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(LeaderboardController), "SubmitCyberGrindScore")]
        [HarmonyPrefix]
        public static bool no(LeaderboardController __instance)
        {
            return false;
        }

        [HarmonyPatch(typeof(LeaderboardController), "SubmitLevelScore")]
        [HarmonyPrefix]
        public static bool nope(LeaderboardController __instance)
        {
            return false;
        }

        [HarmonyPatch(typeof(LeaderboardController), "SubmitFishSize")]
        [HarmonyPrefix]
        public static bool notevenfish(LeaderboardController __instance)
        {
            return false;
        }
    }
}
