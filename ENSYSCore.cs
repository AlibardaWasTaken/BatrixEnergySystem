using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ENSYS
{
    public static class ENSYSCore
    {
        public static Dictionary<Transform, EnergySystem> EnergySystemsUsers = new Dictionary<Transform, EnergySystem>();
        public static Dictionary<Transform, DurabilityTagged> DurabilitySystemsUsers = new Dictionary<Transform, DurabilityTagged>();

        public static bool Initiated = false;
        public static void InitCore()
        {
            if (Initiated == true)
                return;

            Initiated = true;
            Debug.Log("EnCoreSYS ALIVE");

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ENSYS.Resources.HarmonyFixer.dll"))
            {
                byte[] assemblyData;
                using (MemoryStream ms = new MemoryStream())
               {
                    stream.CopyTo(ms);
                    assemblyData = ms.ToArray();
                }
                Assembly loadedAssembly = Assembly.Load(assemblyData);
               Type harmonyFixerType = loadedAssembly.GetType("PP.HarmonyFixer");
                MethodInfo loadMethod = harmonyFixerType.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
                loadMethod.Invoke(null, null); 
            }
        

        new Harmony("Com.Batrix.HTR").PatchAll();
        }

        public static EnergySystem GetEnergySystem(Transform target)
        {
            if (EnergySystemsUsers.ContainsKey(target.root) == true)
            {
                return EnergySystemsUsers[target.root];
            }
            else
            {
                return null;
            }
        }

        [HarmonyPatch]
        public class SlashPatcher
        {

            [HarmonyPatch(typeof(LimbBehaviour), "Slice")]
            [HarmonyPrefix]
            public static bool Prefix(LimbBehaviour __instance)
            {
                if (DurabilitySystemsUsers.ContainsKey(__instance.transform.root))
                {
                    DurabilitySystemsUsers[__instance.transform.root].appliedDurLmb[__instance].Patch_Slice();
                }

                return true;
            }



        }

        [HarmonyPatch]
        public class BloodFootPatcher
        {
            private static readonly ContactPoint2D[] contactBuffer = new ContactPoint2D[8];
            [HarmonyPatch(typeof(LimbBehaviour), "OnCollisionEnter2D")]
            [HarmonyPrefix]
            public static bool Prefix(LimbBehaviour __instance, Collision2D collision)
            {
                if (DurabilitySystemsUsers.ContainsKey(__instance.transform.root))
                {

                    if (DurabilitySystemsUsers[__instance.transform.root].shouldPatchFootBlood == true)
                    {
                        int contacts = collision.GetContacts(contactBuffer);
                        float num = Utils.GetAverageImpulseRemoveOutliers(contactBuffer, contacts, 1f) / 1f * UserPreferenceManager.Current.FragilityMultiplier * DurabilitySystemsUsers[__instance.transform.root].FootBloodMult;
                        Vector2 normal = contactBuffer[0].normal;
                        Vector2 point = contactBuffer[0].point;
                        PhysicalBehaviour physicalBehaviour;
        
                        if (Global.main.PhysicalObjectsInWorldByTransform.TryGetValue(collision.transform, out physicalBehaviour) && physicalBehaviour.SimulateTemperature && physicalBehaviour.Temperature >= 70f)
                        {
                            __instance.Damage(physicalBehaviour.Temperature / 140f);
                            __instance.SkinMaterialHandler.AddDamagePoint(DamageType.Burn, point, physicalBehaviour.Temperature * 0.01f);
                            if (__instance.NodeBehaviour.IsConnectedToRoot && !__instance.IsParalysed)
                            {
                                __instance.Person.AddPain(1f);
                            }
                            __instance.Wince(150f);
                            if (physicalBehaviour.Temperature >= 100f)
                            {
                                __instance.CirculationBehaviour.HealBleeding();
                            }
                        }
                        if (__instance.HasBrain && num > 0.6f && (double)UnityEngine.Random.value > 0.8)
                        {
                            __instance.CirculationBehaviour.InternalBleedingIntensity += num;
                        }
                        if (num < 2f)
                        {
                            return false;
                        }
                        __instance.BruiseCount += 1;
                        PropagateImpactDamage(num, normal, point, 0, __instance, DurabilitySystemsUsers[__instance.transform.root].appliedDurLmb[__instance]);
                        if (num < 1f || __instance.IsAndroid || __instance.CirculationBehaviour.GetAmountOfBlood() < 0.2f)
                        {
                            return false;
                        }
                        if (UnityEngine.Random.value > 0.8f)
                        {
                            __instance.CirculationBehaviour.Cut(point, normal);
                        }
                        if (num < 3f && __instance.Health > __instance.InitialHealth * 0.2f)
                        {
                            return false;
                        }
                        __instance.PhysicalBehaviour.CreateImpactEffect(point, normal, Mathf.Clamp(num / 4f, 1f, 2f));
                        collision.gameObject.SendMessage("Decal", new DecalInstruction(__instance.BloodDecal, point, __instance.CirculationBehaviour.GetComputedColor(__instance.GetOriginalBloodType().Color), 1f), SendMessageOptions.DontRequireReceiver);
                        return false;
                    }
 
                }
                return true;
            }

            private static void PropagateImpactDamage(float impulse, Vector2 direction, Vector2 pos, int iteration, LimbBehaviour origin, DurabilityBaseLimb calledLimb)
            {
                calledLimb.ActOnImpact(impulse, pos);
                if (iteration >= 8)
                {
                    return;
                }
                for (int i = 0; i < calledLimb.limb.ConnectedLimbs.Count; i++)
                {
                    LimbBehaviour limbBehaviour = calledLimb.limb.ConnectedLimbs[i];
                    if (!(limbBehaviour == origin))
                    {
                        float num = Vector2.Dot((calledLimb.transform.position - limbBehaviour.transform.position).normalized, direction);
                        if (num > 0f)
                        {
                            PropagateImpactDamage(num * impulse * 0.9f, direction, pos, iteration + 1, calledLimb.limb, calledLimb);
                        }
                    }
                }
            }


        }



        [HarmonyPatch]
        public class CrushPatcher
        {

            [HarmonyPatch(typeof(LimbBehaviour), "Crush")]
            [HarmonyPrefix]
            public static bool Prefix(LimbBehaviour __instance)
            {
                if (DurabilitySystemsUsers.ContainsKey(__instance.transform.root))
                {
                    DurabilitySystemsUsers[__instance.transform.root].appliedDurLmb[__instance].Patch_Crush();
                }

                return true;
            }



        }

        public static EnergySystem SetUpEnergySystem(Transform target, int MaxEnergy, int RegenEnergy, float RegenTime, bool StartMax, System.Action<GameObject> OnEndAction)
        {
            if (EnergySystemsUsers.ContainsKey(target.root) == false)
            {
                var newsystem = target.root.gameObject.AddComponent<EnergySystem>();
                newsystem.SetMaxEnergy( MaxEnergy);
                newsystem.SetRegen( RegenEnergy);
                newsystem.SetRegenTime( RegenTime);

                if (StartMax == true)
                {
                    newsystem.SetEnergy(MaxEnergy);
                }
                else
                {
                    newsystem.SetEnergy( 0);
                }

                OnEndAction?.Invoke(target.gameObject);
                Debug.Log("Energy System inited");
                return newsystem;
            }
            else
            {
                return GetEnergySystem(target);
            }
        }
    }








}
