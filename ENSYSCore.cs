using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Utility;

namespace ENSYS
{
    public static class ENSYSCore
    {
        public static Dictionary<Transform, EnergySystem> EnergySystemsUsers = new Dictionary<Transform, EnergySystem>();
        public static Dictionary<Transform, LevelSystem> LevelSystemsUsers = new Dictionary<Transform, LevelSystem>();
        public static Dictionary<Transform, DurabilityTagged> DurabilitySystemsUsers = new Dictionary<Transform, DurabilityTagged>();

        public static bool Initiated = false;

        public static enumstarter IEnumStarter;

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

           var enumobj = new GameObject("Enumstr");
            GameObject.DontDestroyOnLoad(enumobj);
            IEnumStarter = enumobj.AddComponent<enumstarter>();
        }

        private static bool IsInited = false;

        public class enumstarter :MonoBehaviour
        {

        }

        public static void InitMain()
        {


            Debug.Log("InitedMain");
            UtilityMethods.FindCanvas();
            UtilityMethods.CacheHumans();

            //if (IsInited == true) return;
            // IsInited = true;
            RegrowthModuleCache.Cache();


            UtilityMethods.DelayedInvoke(0.15f, () =>
            {
                foreach (GameObject Obj in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
                {
                    if(Obj.TryGetComponent(out FirearmBehaviour fire))
                    {
                        Obj.GetOrAddComponent<GunTimeManaged>().Firearm = fire;
                    }
                    if (Obj.TryGetComponent(out PhysicalBehaviour phys))
                    {
                        Obj.GetOrAddComponent<PBTimeManaged>().pb = phys;
                    }
                    if (Obj.TryGetComponent(out ParticleSystem particleSystem))
                    {
                        Obj.GetOrAddComponent<ParticlesTimeManaged>().ParticleSystemComponent = particleSystem;
                    }
                    if (Obj.TryGetComponent(out PersonBehaviour personBehaviour))
                    {
                        Obj.GetOrAddComponent<HumansTimeManaged>().Person = personBehaviour;
                    }
                }

            });

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

        public static LevelSystem GetLevelSystem(Transform target)
        {
            if (LevelSystemsUsers.ContainsKey(target.root) == true)
            {
                return LevelSystemsUsers[target.root];
            }
            else
            {
                return null;
            }
        }



        [HarmonyPatch]
        public class SunDecalPatcher
        {

            [HarmonyPatch(typeof(DecalControllerBehaviour), "Decal")]
            [HarmonyPrefix]
            public static bool Prefix(DecalControllerBehaviour __instance, DecalInstruction instruction)
            {
                // Debug.Log(instruction.colourMultiplier.r + " " + instruction.colourMultiplier.g + " " + instruction.colourMultiplier.b);
                //  Debug.Log("zalupa " + __instance.DecalDescriptor.Color.r + " " + __instance.DecalDescriptor.Color.g + " " + __instance.DecalDescriptor.Color.b);

                if (instruction.colourMultiplier.r > 2f || instruction.colourMultiplier.g > 2f || instruction.colourMultiplier.b > 2f)
                {
                    //Debug.Log("1");
                    if (!UserPreferenceManager.Current.Decals)
                    {
                        return false;
                    }
                    if (__instance.DecalDescriptor != instruction.type)
                    {
                        return false;
                    }
                    if (!__instance.GetField<DecalControllerBehaviour, bool>("dirty"))
                    {
                        //Debug.Log("2");
                        __instance.InvokeMethodRef("CreateContainer");

                        // Debug.Log("3");
                    }
                    // Debug.Log("4");
                    if (!__instance.decalHolder)
                    {
                        return false;
                    }
                    Vector2 vector = __instance.transform.InverseTransformPoint(instruction.globalPosition);
                    if (__instance.InvokeMethodRef<bool>("IsNearOtherDecal", new object[] { vector }))
                    {

                        return false;
                    }
                    // Debug.Log("5");
                    GameObject gameObject = new GameObject("decal");
                    float d = instruction.size * UnityEngine.Random.Range(1f, 1.2f);
                    gameObject.isStatic = true;
                    gameObject.transform.SetParent(__instance.decalHolder.transform, true);
                    gameObject.transform.localScale = __instance.decalHolder.transform.InverseTransformVector(d * Vector3.one);
                    gameObject.transform.localRotation = Quaternion.LookRotation(Vector3.forward, __instance.decalHolder.transform.InverseTransformDirection(Vector3.zero));
                    gameObject.transform.localPosition = vector;
                    SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                    spriteRenderer.sprite = __instance.DecalDescriptor.Sprites.PickRandom<Sprite>();
                    spriteRenderer.color = (__instance.DecalDescriptor.Color * instruction.colourMultiplier) * 0.08f;
                    spriteRenderer.material = ModAPI.FindMaterial("VeryBright");
                    if (UserPreferenceManager.Current.GorelessMode)
                    {
                        SpriteRenderer spriteRenderer2 = spriteRenderer;
                        Color color = spriteRenderer.color;
                        spriteRenderer2.color = Utils.ChangeRedToOrange(color, 0.03f);
                    }
                    spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    spriteRenderer.sortingLayerID = __instance.GetField<DecalControllerBehaviour, ValueTuple<int, int>>("originalSortingLayer").Item1;
                    spriteRenderer.sortingOrder = __instance.GetField<DecalControllerBehaviour, ValueTuple<int, int>>("originalSortingLayer").Item2 + 1;
                    // Debug.Log("6");
                    // if (__instance.DecalDescriptor.GetField<DecalDescriptor, bool>("WillDrip"))
                    // {
                    //  Debug.Log("7");
                    int minInclusive = Mathf.Min(__instance.DecalDescriptor.MaxDripCount, __instance.DecalDescriptor.MinDripCount);
                    int maxExclusive = Mathf.Max(__instance.DecalDescriptor.MaxDripCount, __instance.DecalDescriptor.MinDripCount);
                    for (int i = 0; i <= UnityEngine.Random.Range(minInclusive, maxExclusive); i++)
                    {
                        GameObject gameObject2 = new GameObject("streak");
                        gameObject2.transform.SetParent(gameObject.transform);
                        gameObject2.transform.localPosition = UnityEngine.Random.insideUnitCircle * d / 2f;
                        gameObject2.transform.localScale = UnityEngine.Random.Range(0.02f, 0.05f) * Vector3.one;
                        SpriteRenderer spriteRenderer3 = gameObject2.AddComponent<SpriteRenderer>();
                        spriteRenderer3.sprite = __instance.DecalDescriptor.DripParticle;
                        spriteRenderer3.color = spriteRenderer.color;
                        spriteRenderer3.material = ModAPI.FindMaterial("VeryBright");
                        spriteRenderer3.maskInteraction = spriteRenderer.maskInteraction;
                        spriteRenderer3.sortingLayerID = spriteRenderer.sortingLayerID;
                        spriteRenderer3.sortingOrder = spriteRenderer.sortingOrder;
                        spriteRenderer3.drawMode = SpriteDrawMode.Sliced;
                        gameObject2.AddComponent<DecalDripStreakBehaviour>();
                    }
                    // }
                    __instance.localDecalPositions.Add(vector);
                }
                else
                {

                    return true;
                }


                return false;
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
        public class DamagePatcher
        {

            [HarmonyPatch(typeof(LimbBehaviour), "Damage")]
            [HarmonyPrefix]
            public static bool Prefix(LimbBehaviour __instance, float damage)
            {
                if (DurabilitySystemsUsers.ContainsKey(__instance.transform.root))
                {
                    DurabilitySystemsUsers[__instance.transform.root].appliedDurLmb[__instance].Patch_Damage(damage);
                    return false;
                }
                else
                {
                    return true;
                }


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
                newsystem.SetMaxEnergy(MaxEnergy);
                newsystem.SetRegen(RegenEnergy);
                newsystem.SetRegenTime(RegenTime);

                if (StartMax == true)
                {
                    newsystem.SetEnergy(MaxEnergy);
                }
                else
                {
                    newsystem.SetEnergy(0);
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
