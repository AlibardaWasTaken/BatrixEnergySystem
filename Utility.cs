using ENSYS;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static ENSYS.ENSYSCore;
using static Utility.UtilityMethods;
using Random = UnityEngine.Random;

namespace Utility
{
    public static class TMPFactory
    {
        public static AudioSource CreateAudioSource(this GameObject gameObject, float volume = 1f, float blend = 1f)
        {
            var audioSource = gameObject.AddComponent<AudioSource>();
            AddSoundToGlobal(audioSource, volume, blend);
            return audioSource;
        }


        public static GameObject CreateTMP(Quaternion rotation, Vector3 position, float duration, string textToDisplay, Color textColor, float min = 0.1f, float fontmax = 3f)
        {
            var tmpobj = new GameObject("tmpobj");
            var tmp = tmpobj.AddComponent<TextMeshPro>();

            tmpobj.transform.rotation = rotation;
            tmpobj.transform.position = position;

            // Setup basic properties
            tmp.fontSize = 3;
            tmp.font = ModAPI.FindSpawnable("Holographic Display")
                             .Prefab
                             .transform
                             .Find("Text")
                             .GetComponent<TextMeshPro>().font;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.fontSizeMax = fontmax;
            tmp.fontSizeMin = min;
            tmp.sortingOrder = 20;
            tmp.renderer.sortingLayerName= "Foreground";
            tmp.renderer.sortingOrder = 20;
            tmp.enableAutoSizing = true;
            tmp.text = ""; // Start empty for typewriter effect
            tmp.color = textColor;

            // Add a helper component to handle typewriter and fade logic
            var helper = tmpobj.AddComponent<TypewriterFadeHelper>();
            helper.Initialize(tmp, textToDisplay, duration);

            return tmpobj;
        }

        private class TypewriterFadeHelper : MonoBehaviour
        {
            private TextMeshPro tmp;
            private string fullText;
            private float duration;
            private float typeSpeed = 0.05f; 
            private Coroutine typeCoroutine;

            public void Initialize(TextMeshPro targetTMP, string text, float showDuration)
            {
                tmp = targetTMP;
                fullText = text;
                duration = showDuration;
                typeCoroutine = StartCoroutine(TypewriterRoutine());
            }

            private IEnumerator TypewriterRoutine()
            {
                // Typewriter effect
                tmp.text = "";
                for (int i = 0; i < fullText.Length; i++)
                {
                    tmp.text += fullText[i];
                    yield return new WaitForSeconds(typeSpeed);
                }

            
                yield return new WaitForSeconds(duration);

               
                float fadeTime = 1f; 
                Color initialColor = tmp.color;
                float elapsed = 0f;

                while (elapsed < fadeTime)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                    tmp.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
                    yield return null;
                }

                // Destroy after fully faded
                Destroy(gameObject);
            }
        }
    }


    public static class LerpUtilities
    {




        /// <summary>
        /// Smoothly interpolates a float value over time.
        /// </summary>
        /// <param name="startValue">The initial float value.</param>
        /// <param name="targetValue">The target float value.</param>
        /// <param name="duration">The duration of the interpolation.</param>
        /// <param name="onValueUpdated">Callback for updating the interpolated value during the Lerp.</param>
        /// <param name="onComplete">Callback for when the Lerp is complete.</param>
        public static void SmoothFloatLerp(
            float startValue,
            float targetValue,
            float duration,
            Action<float> onValueUpdated,
            Action onComplete = null)
        {
            ENSYSCore.IEnumStarter.StartCoroutine(FloatLerpCoroutine(startValue, targetValue, duration, onValueUpdated, onComplete));
        }

        private static IEnumerator FloatLerpCoroutine(
            float startValue,
            float targetValue,
            float duration,
            Action<float> onValueUpdated,
            Action onComplete)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float currentValue = Mathf.Lerp(startValue, targetValue, elapsed / duration);
                onValueUpdated?.Invoke(currentValue);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Ensure the final value is set and invoke the completion callback.
            onValueUpdated?.Invoke(targetValue);
            onComplete?.Invoke();
        }




        /// <summary>
        /// Smoothly moves an object from its current position to the target position.
        /// </summary>
        /// <param name="objectToMove">The object to move.</param>
        /// <param name="targetPosition">The position to move to.</param>
        /// <param name="duration">The duration of the movement.</param>
        public static void SmoothMove(Transform objectToMove, Vector3 targetPosition, float duration)
        {
            ENSYSCore.IEnumStarter.StartCoroutine(MoveCoroutine(objectToMove, targetPosition, duration));
        }
        
        public static void RotateAtan(this Transform transf, Vector3 RotatePos, float offset)
        {
            var dirLowFront = transf.transform.position - RotatePos;
            float FrontLowDir = Mathf.Atan2(dirLowFront.y, dirLowFront.x) * Mathf.Rad2Deg;


                FrontLowDir += offset;



            transf.rotation = Quaternion.Euler(0, 0, FrontLowDir);

        }


        private static IEnumerator MoveCoroutine(Transform objectToMove, Vector3 targetPosition, float duration)
        {
            Vector3 startPosition = objectToMove.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                objectToMove.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            objectToMove.position = targetPosition;
        }


        public static void SmoothLocalMove(Transform objectToMove, Vector3 targetPosition, float duration)
        {
            ENSYSCore.IEnumStarter.StartCoroutine(MoveLocalCoroutine(objectToMove, targetPosition, duration));
        }

        private static IEnumerator MoveLocalCoroutine(Transform objectToMove, Vector3 targetPosition, float duration)
        {
            Vector3 startPosition = objectToMove.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                objectToMove.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            objectToMove.localPosition = targetPosition;
        }

        /// <summary>
        /// Smoothly changes the color of a material over time.
        /// </summary>
        /// <param name="renderer">The Renderer whose material color will change.</param>
        /// <param name="targetColor">The color to transition to.</param>
        /// <param name="duration">The duration of the transition.</param>
        public static void SmoothColorChange(SpriteRenderer renderer, Color targetColor, float duration)
        {
            ENSYSCore.IEnumStarter.StartCoroutine(ColorCoroutine(renderer, targetColor, duration));
        }

        private static IEnumerator ColorCoroutine(SpriteRenderer renderer, Color targetColor, float duration)
        {
            Color startColor = renderer.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                renderer.color = Color.Lerp(startColor, targetColor, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            renderer.color = targetColor;
        }

        /// <summary>
        /// Smoothly scales an object over time.
        /// </summary>
        /// <param name="objectToScale">The object to scale.</param>
        /// <param name="targetScale">The scale to transition to.</param>
        /// <param name="duration">The duration of the scaling.</param>
        public static void SmoothScale(Transform objectToScale, Vector3 targetScale, float duration)
        {
            ENSYSCore.IEnumStarter.StartCoroutine(ScaleCoroutine(objectToScale, targetScale, duration));
        }

        private static IEnumerator ScaleCoroutine(Transform objectToScale, Vector3 targetScale, float duration)
        {
            Vector3 startScale = objectToScale.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                objectToScale.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            objectToScale.localScale = targetScale;
        }
    }

    public static class PhysicalMethods
    {
        public static PhysicalBehaviour AddPhysComp(this GameObject gameObject, string propertyname = "Metal", bool EraseSounds = false)
        {
            gameObject.layer = LayerMask.NameToLayer("Objects");
            gameObject.GetOrAddComponent<Rigidbody2D>();
            gameObject.GetOrAddComponent<SpriteRenderer>();
            if (!gameObject.GetComponent<Collider2D>())
            {
                gameObject.GetOrAddComponent<BoxCollider2D>();
            }
            PhysicalBehaviour physicalBehaviour = gameObject.AddComponent<PhysicalBehaviour>();
            physicalBehaviour.Properties = ModAPI.FindPhysicalProperties(propertyname);
            physicalBehaviour.SpawnSpawnParticles = false;
            gameObject.AddComponent<AudioSourceTimeScaleBehaviour>();
            if(EraseSounds == true)
            {
                physicalBehaviour.OverrideShotSounds = Array.Empty<AudioClip>();
                physicalBehaviour.OverrideImpactSounds = Array.Empty<AudioClip>();
            }

            return physicalBehaviour;
        }
    } 

    public static class UtilityMethods
    {



       public static void Destroy( this UnityEngine.Object obj, float delay = 0)
        {
            Destroy(obj, delay);
        }

        public static void CreateFloatDialog(string title, string placeholder, Action<float> output)
        {
            DialogBox dialog = (DialogBox)null;
            dialog = DialogBoxManager.TextEntry(title, placeholder, new DialogButton[]
            {
                new DialogButton("Close", true, () => { }),
                new DialogButton("Enter", true, () =>
                {
                    var result = 0f;
                    float.TryParse(dialog.EnteredText, out result);
                    output.Invoke(result);
                })
            });
            dialog.InputField.contentType = TMP_InputField.ContentType.DecimalNumber;
        }

        public static object InvokeMethodRef(this object obj, string nameMethod, params object[] args)
        {
            return obj.GetType().GetMethod(nameMethod, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(obj, args);
        }
        public static object InvokeMethodRef(this object obj, string nameMethod)
        {
            return obj.GetType().GetMethod(nameMethod, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(obj, Array.Empty<object>());
        }
        public static object InvokeMethodRef(this Type type, string nameMethod)
        {
            return type.GetMethod(nameMethod, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, Array.Empty<object>());
        }
        public static object InvokeMethodRef(this Type type, string nameMethod, params object[] args)
        {
            return type.GetMethod(nameMethod, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, args);
        }
        public static T InvokeMethodRef<T>(this Type type, string nameMethod)
        {
            return (T)InvokeMethodRef(type, nameMethod);
        }
        public static T InvokeMethodRef<T>(this object obj, string nameMethod)
        {
            return (T)InvokeMethodRef(obj, nameMethod);
        }
        public static T InvokeMethodRef<T>(this Type type, string nameMethod, params object[] args)
        {
            return (T)InvokeMethodRef(type, nameMethod, args);
        }
        public static T InvokeMethodRef<T>(this object obj, string nameMethod, params object[] args)
        {
            return (T)InvokeMethodRef(obj, nameMethod, args);

        }

        public static bool IsInfEnergyEnabled = false;
        public static Canvas canvas;



        public static bool CanSay = true;

        public enum KeyWordsTypes
        {
            Shielded,
            Regenerator,
            AdvRegenerator,
            Durable,
            Implant,
            SpecialDur,
        }


        public static void SetPersonlayers(PersonBehaviour person)
        {
            person.Limbs[(int)Utility.LimbIdFromName.Head].gameObject.SetRendererLayer("Default", 7);
            person.Limbs[(int)Utility.LimbIdFromName.UpperBody].gameObject.SetRendererLayer("Default", 5);
            person.Limbs[(int)Utility.LimbIdFromName.MiddleBody].gameObject.SetRendererLayer("Default", 3);
            person.Limbs[(int)Utility.LimbIdFromName.LowerBody].gameObject.SetRendererLayer("Default", 1);
            person.Limbs[(int)Utility.LimbIdFromName.UpperArmFront].gameObject.SetRendererLayer("Foreground", 9);
            person.Limbs[(int)Utility.LimbIdFromName.LowerArmFront].gameObject.SetRendererLayer("Foreground", 7);
            person.Limbs[(int)Utility.LimbIdFromName.UpperLegFront].gameObject.SetRendererLayer("Foreground", 5);
            person.Limbs[(int)Utility.LimbIdFromName.LowerLegFront].gameObject.SetRendererLayer("Foreground", 3);
            person.Limbs[(int)Utility.LimbIdFromName.FootFront].gameObject.SetRendererLayer("Foreground", 1);
            person.Limbs[(int)Utility.LimbIdFromName.UpperArm].gameObject.SetRendererLayer("Background", 3);
            person.Limbs[(int)Utility.LimbIdFromName.LowerArm].gameObject.SetRendererLayer("Background", 1);
            person.Limbs[(int)Utility.LimbIdFromName.UpperLeg].gameObject.SetRendererLayer("Background", 9);
            person.Limbs[(int)Utility.LimbIdFromName.LowerLeg].gameObject.SetRendererLayer("Background", 7);
            person.Limbs[(int)Utility.LimbIdFromName.Foot].gameObject.SetRendererLayer("Background", 5);
        }






        public static string ColorToColorTag(Color SelectedColor)
        {
            if (SelectedColor.Equals(Color.clear))
            {
                SelectedColor = Color.white;
            }
            return "<color=#" + ColorUtility.ToHtmlStringRGB(SelectedColor) + ">";
        }






        public static void SetRendererLayer(this GameObject gameObject, string layername, int layerorder)
        {
            var spr = gameObject.GetComponent<SpriteRenderer>();
            spr.sortingLayerName = layername;
            spr.sortingOrder = layerorder;
        }

        public class RopeDecorManager : MonoBehaviour
        {
            public List<Rigidbody2D> clothPieces = new List<Rigidbody2D>();
            public List<SpriteRenderer> renderers = new List<SpriteRenderer>();

            public bool isDynamic = true;
            public bool IsDunAltMode = true;
            public LimbBehaviour lmb;

            private void Start()
            {
                lmb.gameObject.AddComponent<LinkedRopeDecor>().linked=this;

            }

            public void AddRB(Rigidbody2D rb)
            {
                clothPieces.Add(rb);
                renderers.Add(rb.GetComponent<SpriteRenderer>());
            }

            private void Update()
            {
                if (isDynamic == true)
                {
                    if (IsDunAltMode == true)
                    {
                        foreach (var rend in renderers)
                        {
                            rend.enabled = (lmb.SkinMaterialHandler.AcidProgress < 0.7f && lmb.PhysicalBehaviour.BurnProgress < 0.25f);
                        }
                    }
                    else
                    {
                        foreach (var rend in renderers)
                        {
                            rend.enabled = lmb.Person.IsAlive();
                        }
                    }
                }


                if (Global.main.Paused == true && clothPieces[0].interpolation == RigidbodyInterpolation2D.Interpolate)
                {
                    foreach (var decor in clothPieces)
                    {
                        decor.interpolation = RigidbodyInterpolation2D.None;
                    }
                }
                else if (Global.main.Paused == false && clothPieces[0].interpolation == RigidbodyInterpolation2D.None)
                {
                    foreach (var decor in clothPieces)
                    {
                        decor.interpolation = RigidbodyInterpolation2D.Interpolate;
                    }
                }


            }
        }

        public static bool HaveTag(this GameObject PB, string tag)
        {
            var sys = ENSYSCore.GetEnergySystem(PB.transform.root);
            if (sys == null)
                return false;

            return sys.tagLib.ContainsTag(tag.ToString());
        }

        public static RopeDecorManager SetUpRopeDecor(this GameObject objForAttach, Vector2 connectedAnchor, string layerName, int order, List<Sprite> sprites, float minAngleFP, float maxAngleFP, float minAngle, float maxAngle, float pieceMass = 0.0018f, float distmult = 2.9f)
        {

            Transform holder = new GameObject("RopeDecHolder").transform;
            holder.SetParent(objForAttach.transform, false);
            holder.gameObject.AddComponent<Optout>();

            RopeDecorManager manager = holder.gameObject.AddComponent<RopeDecorManager>();

            if (objForAttach.TryGetComponent<LimbBehaviour>(out var lmb))
            {
                manager.lmb = lmb;
            }

            Rigidbody2D parentrg = objForAttach.GetComponent<Rigidbody2D>();




            int length = sprites.Count;
            HingeJoint2D[] pieces = new HingeJoint2D[length];

            parentrg.transform.localRotation = Quaternion.Euler(Vector3.zero);

            Dictionary<SpriteRenderer, Sprite> ogs = new Dictionary<SpriteRenderer, Sprite>();
            for (int i = 0; i < length; i++)
            {
                GameObject piece = new GameObject("RopeDecor " + i);
                Transform pieceTransform = piece.transform;
                pieceTransform.SetParent(holder);
                pieceTransform.localScale = Vector3.one;
                piece.AddComponent<Optout>();

                Rigidbody2D rigidbody = piece.AddComponent<Rigidbody2D>();
                rigidbody.mass = pieceMass;
                rigidbody.inertia = 0.001f;
                rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;



                Sprite sprite = sprites[i];
                SpriteRenderer spriteRenderer = piece.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
                spriteRenderer.sortingLayerName = layerName;

                manager.AddRB(rigidbody);
                spriteRenderer.sortingOrder = order;

                float AY = (sprite.rect.height / distmult * ModAPI.PixelSize) - (1.5f * ModAPI.PixelSize);

                HingeJoint2D joint = piece.AddComponent<HingeJoint2D>();
                joint.anchor = new Vector2(0f, AY);
                joint.enableCollision = false;
                joint.autoConfigureConnectedAnchor = false;
                pieces[i] = joint;


                ogs.Add(spriteRenderer, sprite);


                if (i != 0)
                {
                    joint.limits = new JointAngleLimits2D
                    {
                        min = minAngle,
                        max = maxAngle
                    };

                    HingeJoint2D previousPiece = pieces[i - 1];
                    joint.connectedBody = previousPiece.attachedRigidbody;
                    joint.connectedAnchor = -previousPiece.anchor;
                    pieceTransform.localPosition = (Vector2)previousPiece.transform.localPosition - previousPiece.anchor - joint.anchor;
                }
                else
                {
                    pieceTransform.localPosition = connectedAnchor;
                    joint.connectedBody = parentrg;
                    joint.connectedAnchor = connectedAnchor;
                    spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

                    joint.limits = new JointAngleLimits2D
                    {
                        min = minAngleFP,
                        max = maxAngleFP
                    };
                }
            }
            return manager;
        }




        public class SkinDataSet
        {
            public Sprite HeadSpr { get; set; }
            public Sprite UpTorsoSpr { get; set; }
            public Sprite MiddleTorso { get; set; }
            public Sprite DownTorso { get; set; }
            public Sprite UpArm { get; set; }
            public Sprite DownArm { get; set; }
            public Sprite UpLeg { get; set; }
            public Sprite DownLeg { get; set; }
            public Sprite Foot { get; set; }
        }





        public static SkinDataSet CreateSkinData(Sprite headSpr, Sprite upTorsoSpr, Sprite middleTorsoSpr, Sprite downTorsoSpr,
                                            Sprite upArmSpr, Sprite downArmSpr, Sprite upLegSpr, Sprite downLegSpr, Sprite footSpr)
        {
            return new SkinDataSet
            {
                HeadSpr = headSpr,
                UpTorsoSpr = upTorsoSpr,
                MiddleTorso = middleTorsoSpr,
                DownTorso = downTorsoSpr,
                UpArm = upArmSpr,
                DownArm = downArmSpr,
                UpLeg = upLegSpr,
                DownLeg = downLegSpr,
                Foot = footSpr
            };
        }

        public static ShakeUiNotif SetShakeNotif()
        {
            var area = UtilityMethods.canvas.transform.Find("NotificationArea");
            return area.transform.GetChild(area.childCount - 1).gameObject.GetOrAddComponent<ShakeUiNotif>();
        }

        public static void ChangeSkinSliced(this PersonBehaviour person, SkinDataSet skinData, SkinDataSet FlData = null, SkinDataSet SkData = null, Texture2D damage = null, bool fixcol = true)
        {
            if(Initiated == false)
            {
                ENSYSCore.InitCore();
            }

            person.gameObject.GetOrAddComponent<BeingSliced>();
            person.Limbs[0].SetSlicedSkinForLimb(skinData.HeadSpr, FlData?.HeadSpr, SkData?.HeadSpr, damage);

            person.Limbs[1].SetSlicedSkinForLimb(skinData.UpTorsoSpr, FlData?.UpTorsoSpr, SkData?.UpTorsoSpr, damage);
            person.Limbs[2].SetSlicedSkinForLimb(skinData.MiddleTorso, FlData?.MiddleTorso, SkData?.MiddleTorso, damage);
            person.Limbs[3].SetSlicedSkinForLimb(skinData.DownTorso, FlData?.DownTorso, SkData?.DownTorso, damage);

            person.Limbs[4].SetSlicedSkinForLimb(skinData.UpLeg, FlData?.UpLeg, SkData?.UpLeg, damage);
            person.Limbs[7].SetSlicedSkinForLimb(skinData.UpLeg, FlData?.UpLeg, SkData?.UpLeg, damage);
            person.Limbs[5].SetSlicedSkinForLimb(skinData.DownLeg, FlData?.DownLeg, SkData?.DownLeg, damage);
            person.Limbs[8].SetSlicedSkinForLimb(skinData.DownLeg, FlData?.DownLeg, SkData?.DownLeg, damage);
            person.Limbs[6].SetSlicedSkinForLimb(skinData.Foot, FlData?.Foot, SkData?.Foot, damage);
            person.Limbs[9].SetSlicedSkinForLimb(skinData.Foot, FlData?.Foot, SkData?.Foot, damage);

            person.Limbs[10].SetSlicedSkinForLimb(skinData.UpArm, FlData?.UpArm, SkData?.UpArm, damage);
            person.Limbs[12].SetSlicedSkinForLimb(skinData.UpArm, FlData?.UpArm, SkData?.UpArm, damage);
            person.Limbs[11].SetSlicedSkinForLimb(skinData.DownArm, FlData?.DownArm, SkData?.DownArm, damage);
            person.Limbs[13].SetSlicedSkinForLimb(skinData.DownArm, FlData?.DownArm, SkData?.DownArm, damage);


            // if (damage != null)
            // {
            //    //limb.SkinMaterialHandler.renderer.material.SetTexture("_DamageTex", damage);
            //}
            if (fixcol)
            {
                DelayedInvoke(0.1f, () =>
                {
                    foreach (var lmb in person.Limbs)
                    {
                        lmb.gameObject.FixColliders();
                    }
                    person.gameObject.NoChildCollide();

                    foreach (var gripBeh in person.GetComponentsInChildren<GripBehaviour>())
                    {
                        gripBeh.CollidersToIgnore = gripBeh.transform.root.GetComponentsInChildren<Collider2D>().ToList();
                    }
                });
            }
        }



        public static void DelayedFrameInvoke(Action action)
        {
            new GameObject("ENSYS_FrameDELAY").AddComponent<DelayerFrameCaller>().Init(action);
        }

        public static void MakeDamagableCloth(this GameObject obj, GameObject toconnect , float mult = 0.4f)
        {
            var spr = obj.GetComponent<SpriteRenderer>();
            var limb = toconnect.GetComponent<LimbBehaviour>();
            if (spr == null || limb == null)
            {
                return;
            }

            var l =spr.gameObject.AddComponent<DamagableLinked>();
            l.limb = limb;
            l.spr = spr;
            l.DamageMultiplier = mult;

        }

        public class DamagableLinked : MonoBehaviour
        {
            public LimbBehaviour limb;
            public SpriteRenderer spr;
            public static Material humanMat;
            public float DamageMultiplier = 0.4f;
            public void Start()
            {
                if(humanMat == null)
                {
                    humanMat = Instantiate(ModAPI.FindSpawnable("Human").Prefab.transform.Find("Head").GetComponent<SpriteRenderer>().material);
                    humanMat.SetTexture("_MainTex", null);
                    humanMat.SetTexture("_FleshTex", null);
                    humanMat.SetTexture("_BoneTex", null);
                    humanMat.SetFloat("_DamageMultiplier", 0.6f);
                   
                }
                var newmat = Instantiate(humanMat);
                newmat.SetTexture("_MainTex", spr.sprite.texture);
                newmat.SetFloat("_DamageMultiplier", DamageMultiplier);
                newmat.SetColor("_BloodColor", limb.CirculationBehaviour.GetComputedColor());
                spr.material = newmat;
            }

            protected  void Update()
            {
                if(limb == null)
                {
                    Destroy(this);
                }

                spr.material.SetFloat("_AcidProgress", limb.SkinMaterialHandler.renderer.material.GetFloat("_AcidProgress"));
                spr.material.SetFloat("_BurnProgress", limb.SkinMaterialHandler.renderer.material.GetFloat("_BurnProgress"));


                spr.material.SetVectorArray(ShaderProperties.Get("_DamagePoints"), limb.SkinMaterialHandler.renderer.material.GetVectorArray(ShaderProperties.Get("_DamagePoints")));
                spr.material.SetInt(ShaderProperties.Get("_DamagePointCount"), limb.SkinMaterialHandler.renderer.material.GetInt(ShaderProperties.Get("_DamagePointCount")));
                


            }
        }


        public static void SetSlicedSkinForLimb(this LimbBehaviour limb, Sprite spr, Sprite flsprite, Sprite sksprite, Texture2D damage, float scale = 1)
        {
           // Debug.Log(limb.name);
            Sprite oldSprite;
            Vector2 oldSpriteSize = Vector3.zero;
            Vector2 oldAnchor = Vector3.zero;
            Vector2 oldPivot = Vector3.zero;
            Vector2 normalizedAnchorPosition = Vector3.zero;
            Vector2 oldAnchorRelativeToSprite = Vector3.zero;
            Vector2 oldAnchorWorldPosition;
            Vector2 oldConnectedAnchorRelativeToSprite = Vector3.zero;
            Vector2 oldConnectedAnchor = Vector3.zero;
            Vector2 normalizedConnectedAnchorPosition = Vector3.zero;
            float oldPixelsPerUnit;
            if (limb.HasJoint)
            {
                HingeJoint2D hingeJoint = limb.Joint;
                SpriteRenderer spriteRenderer = limb.SkinMaterialHandler.renderer;
                hingeJoint.autoConfigureConnectedAnchor = false;

                oldSprite = spriteRenderer.sprite;
                oldPixelsPerUnit = oldSprite.pixelsPerUnit;
                oldSpriteSize = oldSprite.rect.size / oldPixelsPerUnit;
                oldPivot = oldSprite.pivot / oldPixelsPerUnit;

                // Store the old anchor position
                oldAnchor = hingeJoint.anchor;

                oldConnectedAnchor = hingeJoint.connectedAnchor;

                // Calculate the connected anchor's position relative to the sprite's bottom-left corner
                oldConnectedAnchorRelativeToSprite = oldConnectedAnchor + oldPivot - (oldSpriteSize / 2f);

                // Calculate the normalized position of the connected anchor within the old sprite
                normalizedConnectedAnchorPosition = new Vector2(
                    oldConnectedAnchorRelativeToSprite.x / oldSpriteSize.x,
                    oldConnectedAnchorRelativeToSprite.y / oldSpriteSize.y
                );


                // Calculate the old anchor's world position
                oldAnchorWorldPosition = limb.transform.TransformPoint(oldAnchor);

                // Calculate the anchor's position relative to the sprite's bottom-left corner
                oldAnchorRelativeToSprite = oldAnchor + oldPivot - (oldSpriteSize / 2f);

                // Calculate the normalized position of the anchor within the old sprite
                normalizedAnchorPosition = new Vector2(
                   oldAnchorRelativeToSprite.x / oldSpriteSize.x,
                   oldAnchorRelativeToSprite.y / oldSpriteSize.y
               );

            }
           // Debug.Log("PassedJoint");
            var originalSprite = limb.SkinMaterialHandler.renderer.sprite;
            LimbSpriteCache.Key key = new LimbSpriteCache.Key(spr, spr.texture, flsprite.texture, sksprite.texture, scale);
            //Debug.Log(flsprite.bounds);
            if (LimbSpriteCache.Instance.Sprites.TryGetValue(key, out var skval))
            {
               // Debug.Log("Key Present, continue generating texture");
            }
            else
            {
                skval = new LimbSpriteCache.LimbSprites(SlicePivotGrabber(spr.texture, spr), SlicePivotGrabber(flsprite.texture, flsprite), SlicePivotGrabber(sksprite.texture, sksprite));
                LimbSpriteCache.Instance.Sprites.Add(key, skval);
            }
           // Debug.Log("Passedcache");
            LimbSpriteCache.LimbSprites limbSprites = skval;
            limb.SkinMaterialHandler.renderer.sprite = limbSprites.Skin;

           // Debug.Log("Passedskin");
            if (flsprite != null)
            {
                limb.SkinMaterialHandler.renderer.material.SetTexture("_FleshTex", flsprite.texture);
            }
            //Debug.Log("Passedfl");
            if (sksprite != null)
            {
                limb.SkinMaterialHandler.renderer.material.SetTexture("_BoneTex", sksprite.texture);
            }
           // Debug.Log("Passedbone");
            if (damage != null)
            {
                limb.SkinMaterialHandler.renderer.material.SetTexture("_DamageTex", damage);
            }
           // Debug.Log("Passeddamagetex");
            ShatteredObjectSpriteInitialiser shatteredObjectSpriteInitialiser;
            if (limb.TryGetComponent<ShatteredObjectSpriteInitialiser>(out shatteredObjectSpriteInitialiser))
            {
                shatteredObjectSpriteInitialiser.UpdateSprites(limbSprites);
            }
           // Debug.Log("Passedshattered");



            if (limb.HasJoint)
            {

                ENSYSCore.IEnumStarter.StartCoroutine(jointfixcor());




            }

            IEnumerator jointfixcor()
            {
               

                //while (Time.timeScale < 0.98f || Global.main.Paused == true)
                //{
                //   yield return null;
                //}


                HingeJoint2D hingeJoint = limb.Joint;


                // Get the SpriteRenderer component
                SpriteRenderer spriteRenderer = limb.SkinMaterialHandler.renderer;


                Sprite newSpriteInstance = spriteRenderer.sprite;
                float newPixelsPerUnit = newSpriteInstance.pixelsPerUnit;
                Vector2 newSpriteSize = newSpriteInstance.rect.size / newPixelsPerUnit;
                Vector2 newPivot = newSpriteInstance.pivot / newPixelsPerUnit;

                // Calculate the new anchor position relative to the new sprite
                Vector2 newAnchorRelativeToSprite = new Vector2(
                    normalizedAnchorPosition.x * newSpriteSize.x,
                    normalizedAnchorPosition.y * newSpriteSize.y
                );

                // Calculate the new anchor position in local space
                Vector2 newAnchor = newAnchorRelativeToSprite - newPivot + (newSpriteSize / 2f);

                // Update the hinge joint's anchor
                hingeJoint.anchor = newAnchor;

                // Calculate the new anchor's world position
                Vector2 newAnchorWorldPosition = limb.transform.TransformPoint(newAnchor);



                Vector2 newConnectedAnchorRelativeToSprite = new Vector2(
        normalizedConnectedAnchorPosition.x * newSpriteSize.x,
        normalizedConnectedAnchorPosition.y * newSpriteSize.y
    );

                // Calculate the new connected anchor position in local space
                Vector2 newConnectedAnchor;
                newConnectedAnchor = newConnectedAnchorRelativeToSprite - newPivot + (newSpriteSize / 2f);
                if (limb.name.Contains("LowerLeg"))
                {
                    newConnectedAnchor *= 0.86f;
                }


                // Update the hinge joint's connected anchor
                hingeJoint.connectedAnchor = newConnectedAnchor;



                // limb.gameObject.AddComponent<DebugDrawJoint>();
                limb.PhysicalBehaviour.RefreshOutline();
               // Debug.Log("Passedrefresh");
                yield return null;

                if(limb.Person.TryGetComponent<BeingSliced>(out var sl))
                {
                    GameObject.Destroy(sl);
                }
               // Debug.Log("Passedresliced");

                // limb.gameObject.GetOrAddComponent<HingRestore>();


            }
        }

        public class HingRestore : MonoBehaviour
        {
 
            public IEnumerator Start()
            {
                yield return null;
             if(this.transform.root.parent == null)
                {

                    var hingeJoint = this.GetComponent<HingeJoint2D>();
                    hingeJoint.autoConfigureConnectedAnchor = true;
                }


            }


        }


        public static Sprite SlicePivotGrabber(Texture2D SpriteTesture, Sprite SpriteItself)
        {
            var normalizedPivot = new Vector2(
                SpriteItself.pivot.x / SpriteItself.rect.width,
                SpriteItself.pivot.y / SpriteItself.rect.height
            );
            return Sprite.Create(SpriteTesture, SpriteItself.rect, normalizedPivot, SpriteItself.pixelsPerUnit);
        }
        public static string GetHierarchyPath(Transform startTransform, Transform endTransform)
        {
            try
            {
                string hierarchyPath = endTransform.name;
                if (endTransform.root == startTransform.root)
                {
                    Transform lastTransform = endTransform;
                    while (true)
                    {
                        if (lastTransform.parent == endTransform.root)
                        {
                            break;
                        }
                        hierarchyPath = lastTransform.parent.name + "/" + hierarchyPath;
                        lastTransform = lastTransform.parent;
                        if (lastTransform == startTransform)
                        {
                            break;
                        }
                    }
                    return hierarchyPath;
                }
                return hierarchyPath;
            }
            catch
            {
                return "";
            }
        }

        public static void AddSoundToGlobal(AudioSource Source, float volume = 1, float spatialBlend = 1)
        {
            if (Source == null)
            {
                return;
            }

            GameObject.FindObjectOfType<Global>().AddAudioSource(Source, false);
            Source.outputAudioMixerGroup = GameObject.FindObjectOfType<Global>().SoundEffects;

            Source.spatialBlend = spatialBlend;
            Source.volume = volume;
        }



        public static void AddGameobjSoundToGlobal(GameObject obj, float volume = 1, float spatialBlend = 1)
        {
            AudioSource Source = obj.GetComponent<AudioSource>();
            if (Source == null)
            {
                return;
            }

            GameObject.FindObjectOfType<Global>().AddAudioSource(Source, false);
            Source.outputAudioMixerGroup = GameObject.FindObjectOfType<Global>().SoundEffects;

            Source.spatialBlend = spatialBlend;
            Source.volume = volume;
        }








        public static void NoChildCollide(this GameObject instance)
        {
            Collider2D[] componentsInChildren = instance.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D collider2D in componentsInChildren)
            {
                foreach (Collider2D collider2D2 in componentsInChildren)
                {
                    if (collider2D && collider2D2 && collider2D != collider2D2)
                    {
                        Physics2D.IgnoreCollision(collider2D, collider2D2);
                    }
                }
            }
        }



        public static ReactionData CreateReactionData(List<string> lines, float delay = 2f, System.Action<GameObject> OnEnd = null)
        {
            var data = new ReactionData();
            data.Lines = lines;
            data.delay = delay;
            data.OnEnd = OnEnd;
            return data;
        }



        public static void CreateComplexReaction(List<string> lines, string nameToAdd, float delay, ref ReactionToSomething reaction, System.Action<GameObject> OnEnd = null)
        {
            var data = UtilityMethods.CreateReactionData(lines, delay, OnEnd);
            reaction.ReactionsDic.Add(nameToAdd, data);
        }

        public static void CreateReaction(ref ReactionToSomething reaction, string nameToAdd, ReactionData Data)
        {
            reaction.ReactionsDic.Add(nameToAdd, Data);
        }

        public static void DelayedInvoke(float delay, Action action)
        {
            new GameObject("ENSYS_DELAY").AddComponent<DelayerCaller>().Init(action, delay);
        }

        public static void SetField<T>(this T obj, string nameField, object value)
        {
            typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).Where(field => field.Name == nameField).FirstOrDefault().SetValue(obj, value);
        }

        public static A GetField<T, A>(this T obj, string nameField)
        {
            return (A)typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).Where(field => field.Name == nameField).FirstOrDefault().GetValue(obj);
        }

        public static void FindCanvas()
        {
            canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        }

        public static void CacheHumans()
        {
            if(PersonRecorder.RecordedPersons == null)
            {
                PersonRecorder.RecordedPersons = new HashSet<PersonBehaviour>();
                PersonRecorder.PersonsRoots = new Dictionary<Transform, PersonBehaviour>();


            }
            ModAPI.OnItemSpawned += ChachePers;

        }

        private static void ChachePers(object sender, UserSpawnEventArgs e)
        {
            if (e.Instance.GetComponent<PersonBehaviour>() != null) {
                e.Instance.GetOrAddComponent<PersonRecorder>();


            }
        }

        public static void OneTimeAction(this GameObject gameObject, UnityAction Action)
        {
            if (!gameObject.GetComponent<OnCopyFixer>())
                Action.Invoke();

            gameObject.GetOrAddComponent<OnCopyFixer>();
        }



        public static Gradient rainbowGradient;

        public static Gradient CreateRainbowGradient()
        {
            if (rainbowGradient == null)
            {
                Gradient gradient = new Gradient();

                GradientColorKey[] colorKeys = new GradientColorKey[7];
                colorKeys[0] = new GradientColorKey(Color.red, 0f);
                colorKeys[1] = new GradientColorKey(new Color(1f, 0.5f, 0f), 1f / 6f);
                colorKeys[2] = new GradientColorKey(Color.yellow, 2f / 6f);
                colorKeys[3] = new GradientColorKey(Color.green, 3f / 6f);
                colorKeys[4] = new GradientColorKey(Color.blue, 4f / 6f);
                colorKeys[5] = new GradientColorKey(new Color(0.29f, 0f, 0.51f), 5f / 6f);
                colorKeys[6] = new GradientColorKey(Color.magenta, 1f);

                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                alphaKeys[1] = new GradientAlphaKey(1f, 1f);

                gradient.SetKeys(colorKeys, alphaKeys);
                rainbowGradient = gradient;
                return gradient;
            }
            else
            {
                return rainbowGradient;
            }
        }



        public static GameObject SetUpDynamicDecor(this GameObject Object, Vector3 pos, Vector3 scale, LimbBehaviour limb, Sprite sprite = null, bool IsGlow = true, Color col = default, bool IsinAltMod = false)
        {
            GameObject DecorationObj = new GameObject("Decor" + Random.Range(-1000, 1000).ToString());

            DecorationObj.transform.parent = Object.transform;
            DecorationObj.transform.localPosition = pos;
            DecorationObj.transform.localScale = scale;
            DecorationObj.transform.localRotation = Quaternion.Euler(Vector3.zero);
            DecorationObj.AddComponent<Optout>();

            var DecorSprite = DecorationObj.AddComponent<SpriteRenderer>();
            DecorSprite.sprite = sprite;

            DecorationObj.AddComponent<RendererEnabler>();
            DecorSprite.color = col;
            LightSprite li = null;
            if (IsGlow == true)
            {
                DecorSprite.material = ModAPI.FindMaterial("VeryBright");
                li = ModAPI.CreateLight(DecorationObj.transform, new Color(col.r, col.g, col.b, 1f), 0.5f, 0.6f);
            }

            DecorSprite.sortingOrder = Object.GetComponent<SpriteRenderer>().sortingOrder + 1;
            DecorSprite.sortingLayerName = Object.GetComponent<SpriteRenderer>().sortingLayerName;

            var dd = limb.Person.gameObject.AddComponent<DynamicDecoration>();
            dd.Init(limb, DecorationObj, IsinAltMod);
            dd.sprrend = DecorSprite;
            dd.Lsprite = li;



            DecorationObj.AddComponent<LinkedDynamicDecor>().Linked = dd;
            limb.gameObject.AddComponent<LinkedDynamicDecor>().Linked = dd;
            return DecorationObj;
        }

        public static List<GameObject> GetAllChildren(GameObject obj)
        {
            List<GameObject> children = new List<GameObject>();

            foreach (Transform child in obj.transform)
            {
                children.Add(child.gameObject);
                children.AddRange(GetAllChildren(child.gameObject));
            }

            return children;
        }

        public static BurnAffected MakeBurnable(this GameObject obj, GameObject linkTo, bool shouldcopy = true, float burnmult = 1f)
        {
            if(obj.GetComponent<SpriteRenderer>() == null)
            {
                Debug.LogError("There is no SRenderer on obj");
                return null;

            }


            if (linkTo.TryGetComponent<PhysicalBehaviour>(out var ph) == false)
            {
                Debug.LogError("There is no PB on obj");
                return null;

            }


            var cloth = obj.gameObject.AddComponent<BurnAffected>();
            cloth.LinkedPhys = ph;
            cloth.Shouldcopy = shouldcopy;
            cloth.mult = burnmult;

            return cloth;
        }
        public static GameObject SetUpPhysDecor(this GameObject Object, Vector3 pos, Vector3 scale, LimbBehaviour limb, Sprite sprite = null, bool IsGlow = true, Color col = default, float seconds = 0.1f)
        {
            GameObject DecorationObj = ModAPI.CreatePhysicalObject("PhysDecor" + Random.Range(-1000, 1000).ToString(), sprite);
            DecorationObj.transform.parent = Object.transform;
            DecorationObj.transform.localPosition = pos;
            DecorationObj.transform.localScale = scale;
            DecorationObj.transform.localRotation = Quaternion.Euler(Vector3.zero);
            DecorationObj.FixColliders();

            DecorationObj.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Insulator");
            var DecorSprite = DecorationObj.GetComponent<SpriteRenderer>();

            DecorationObj.AddComponent<RendererEnabler>();

            if (IsGlow == true)
                DecorSprite.material = ModAPI.FindMaterial("VeryBright");

            DecorSprite.color = col;

            DecorSprite.sortingOrder = Object.GetComponent<SpriteRenderer>().sortingOrder + 1;
            DecorSprite.sortingLayerName = Object.GetComponent<SpriteRenderer>().sortingLayerName;

            var pd = limb.Person.gameObject.AddComponent<PhysDecoration>();
            pd.Init(limb, DecorationObj, seconds);
            limb.gameObject.AddComponent<LinkedPhysDecor>().linked = pd;
            return DecorationObj;
        }





        public static GameObject SetUpRegularDecor(this GameObject Object, Vector3 pos, Vector3 scale, Sprite sprite = null, bool IsGlow = true, Color col = default)
        {
            GameObject DecorationObj = new GameObject("Decor" + Random.Range(-1000, 1000).ToString());
            DecorationObj.transform.parent = Object.transform;
            DecorationObj.transform.localPosition = pos;
            DecorationObj.transform.localScale = scale;
            DecorationObj.transform.localRotation = Quaternion.Euler(Vector3.zero);
            DecorationObj.AddComponent<Optout>();

            var DecorSprite = DecorationObj.AddComponent<SpriteRenderer>();
            DecorSprite.sprite = sprite;

            if (IsGlow == true)
                DecorSprite.material = ModAPI.FindMaterial("VeryBright");

            DecorSprite.color = col;

            DecorSprite.sortingOrder = Object.GetComponent<SpriteRenderer>().sortingOrder + 1;
            DecorSprite.sortingLayerName = Object.GetComponent<SpriteRenderer>().sortingLayerName;

            return DecorationObj;
        }

        public static void RegenLimb(this LimbBehaviour Limb)
        {
            Limb.CirculationBehaviour.AddLiquid(Limb.GetOriginalBloodType(), (Limb.CirculationBehaviour.Limits.y - Limb.CirculationBehaviour.GetAmountOfBlood()) * 0.4f);
            Limb.CirculationBehaviour.BloodFlow = 1f;

            Limb.Numbness = 0f;

            Limb.HealBone();

            Limb.LungsPunctured = false;

            Limb.CirculationBehaviour.HealBleeding();

            Limb.CirculationBehaviour.IsPump = Limb.CirculationBehaviour.WasInitiallyPumping;

            Limb.Health = Limb.InitialHealth;

            Limb.InternalTemperature = Limb.BodyTemperature;
        }

        public static IEnumerator BurnLimb(this LimbBehaviour Limb, float amount, float time = 10f)
        {
            var val = Mathf.Clamp(Limb.PhysicalBehaviour.BurnProgress + amount, 0f, 0.98f);
            var timer = time;

            while (Limb.PhysicalBehaviour.BurnProgress < val)
            {
                Limb.PhysicalBehaviour.BurnProgress += 0.1f * Time.deltaTime;
                timer -= Time.deltaTime;
                if (timer < 0)
                    break;
                yield return null;
            }
        }

        public class BeingSliced : MonoBehaviour
        {
        }
    }

    public class PersonRecorder : MonoBehaviour
    {
        public static HashSet<PersonBehaviour> RecordedPersons;
        public static Dictionary<Transform,PersonBehaviour> PersonsRoots;

        private void Start()
        {
            var person = this.gameObject.GetComponent<PersonBehaviour>();
            RecordedPersons.Add(person);
            PersonsRoots.Add(this.transform, person);
        }
        private void OnDestroy()
        {
            RecordedPersons.Remove(this.gameObject.GetComponent<PersonBehaviour>());
            PersonsRoots.Remove(this.transform);
        }

    }

    public abstract class AbstractDecor : MonoBehaviour
    {

    }

    public class BurnAffected : MonoBehaviour
    {
        public static Material Material;
        public float mult = 1f;
        public PhysicalBehaviour LinkedPhys
        {
            get
            {
                return _phys;
            }
            set
            {
                if (value == null)
                {
                    Shouldcopy = false;
                    return;
                }
                Shouldcopy = true;
                _phys = value;
            }
        }
        [SerializeField]
        private PhysicalBehaviour _phys;
      
        public bool Shouldcopy = false;
        public float BurnProgress
        {
            get
            {
                if (Shouldcopy) return Mathf.Clamp01(LinkedPhys.BurnProgress);
                return Mathf.Clamp01(NonCopyProg);
            }
            set
            {
                if (Shouldcopy) return;
                NonCopyProg = value;
            }
        }

        public float NonCopyProg;
        [SerializeField]
        private SpriteRenderer spriteRenderer;
        private void Start()
        {
            if (Material == null)
            {
                Material = ModAPI.FindSpawnable("Crate").Prefab.GetComponent<SpriteRenderer>().material;
            }
            spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            spriteRenderer.material = Material;
        }
        private void Update()
        {
            spriteRenderer.material.SetFloat("_Progress", BurnProgress * mult);
        }
    }

    public class DynamicDecoration : AbstractDecor
    {
        public float BurnHold = 0.5f;
        public float AcidHold = 0.4f;
        private LimbBehaviour limb;
        [SkipSerialisation]
        public GameObject _obj;
        public bool IsinAltMod;
        public LightSprite Lsprite;
        public bool IsActive = true;
        public SpriteRenderer sprrend;


        public void Init(LimbBehaviour Fromlimb, GameObject obj, bool isinAltMod = false)
        {
            limb = Fromlimb;
            _obj = obj;
            IsinAltMod = isinAltMod;


        }


        public void ChangeColor(Color color)
        {
            if (sprrend != null)
                sprrend.color = color;

            if (Lsprite != null)
                Lsprite.Color = new Color(color.r, color.g, color.b, 1f);
        }

        private void FixedUpdate()
        {


            if (_obj != null)
            {
                if (IsActive == false)
                {
                    _obj.SetActive(false);
                    return;
                }

                if (IsinAltMod == true)
                {
                    var hueta = limb.SkinMaterialHandler.AcidProgress < AcidHold && limb.PhysicalBehaviour.BurnProgress < BurnHold;
                   // Debug.Log(hueta);
                    _obj.SetActive(hueta);
                    return;
                }

                _obj.SetActive(limb.IsConsideredAlive);
                return;
            }

            Destroy(this);
        }
    }

    public class PhysDecoration : AbstractDecor
    {
        public LimbBehaviour limb;
        [SkipSerialisation]
        public GameObject _obj;
        private Rigidbody2D rg;
        private bool _inprog = false;
        private float seconds;

        public void Init(LimbBehaviour Fromlimb, GameObject obj, float sec)
        {
            limb = Fromlimb;
            _obj = obj;
            seconds = sec;

            rg = _obj.GetComponent<Rigidbody2D>();
            rg.simulated = false;
            rg.gravityScale = 0f;
            _obj.layer = LayerMask.NameToLayer("Debris");
            _obj.GetComponent<PhysicalBehaviour>().DisplayBloodDecals = true;

            //_obj.AddComponent<Undraggable>();
        }

        private void FixedUpdate()
        {
            if (_inprog == true)
                return;

            if (_obj != null && limb != null)
            {
                if (!limb.IsConsideredAlive)
                {
                    _inprog = true;

                    this.StartCoroutine(TakeOff(limb.PhysicalBehaviour.rigidbody.velocity.normalized));
                }
                return;
            }

            Destroy(this);
        }

        public void TakeOffDecor(Vector3 force)
        {
            this.StartCoroutine(TakeOff(force));
        }

        private IEnumerator TakeOff(Vector3 force)
        {
            yield return new WaitForSeconds(seconds);
            rg.simulated = true;
            rg.gravityScale = 1f;
            rg.velocity = limb.PhysicalBehaviour.rigidbody.velocity;

            _obj.transform.parent = null;
            //_obj.FixColliders();
            var Phys = _obj.GetComponent<PhysicalBehaviour>();
            Phys.RecalculateMassBasedOnSize();
            Phys.RefreshOutline();
            UtilityMethods.DelayedFrameInvoke(() => { Phys.rigidbody.AddForce(force, ForceMode2D.Impulse); });

            _obj.GetOrAddComponent<DebrisComponent>();
            // Destroy(_obj.GetComponent<Undraggable>());
            Destroy(this);
        }
    }



    public static class ABloader
    {
        private static Type _ABType;
        private static MethodInfo _loadFromFile;
        private static MethodInfo _loadFromBundle;
        private static MethodInfo _unloadBundle;

        public static T LoadFromAB<T>(object AB, string name)
        {
            return (T)_loadFromBundle.Invoke(AB, new object[] { name, typeof(T) });
        }

        public static object LoadFromFile(string patch)
        {
            return _loadFromFile.Invoke(null, new object[] { patch });
        }

        public static void UnloadAB(object AB)
        {
            _unloadBundle.Invoke(AB, new object[] { false });
        }

        public static Assembly DllAcitvator(string path)
        {
            var Loaderbytes = System.IO.File.ReadAllBytes(path);
            return Assembly.Load(Loaderbytes);
        }

        static ABloader()
        {
            _ABType = Type.GetType("UnityEngine.AssetBundle, UnityEngine.AssetBundleModule");

            _loadFromFile = _ABType.GetMethod("LoadFromFile", new[] { typeof(string) });
            _loadFromBundle = _ABType.GetMethod("LoadAsset", new[] { typeof(string), typeof(Type) });
            _unloadBundle = _ABType.GetMethod("Unload");
        }
    }

    public class OnCopyFixer : MonoBehaviour
    {
        private void OnDisable()
        {
            try { Destroy(this); }
            catch { }

            gameObject.GetOrAddComponent<OnCopyFixer>();
        }
    }








    public class SnowDisabler : MonoBehaviour
    {
        public void Awake()
        {
            this.gameObject.tag = "NoClear";
            this.gameObject.layer = LayerMask.NameToLayer("Bounds");
        }

        private void LateUpdate()
        {
            var loadermap = UnityEngine.Object.FindObjectOfType<MapLoaderBehaviour>().gameObject.transform.GetChild(0).gameObject;
            if (loadermap != null && loadermap.activeInHierarchy == true)
                loadermap.SetActive(false);
        }
    }

    public struct SerializedObjects
    {
        public List<ObjectState> Objects;
    }

    public class DelayerCaller : MonoBehaviour
    {
        private Action _ActionForInvoke;
        private float _Delay;

        public void Init(Action actionForInvoke, float delay)
        {
            _ActionForInvoke = actionForInvoke;
            _Delay = delay;
        }

        private void Start()
        {
            StartCoroutine(Delayer());
        }

        private IEnumerator Delayer()
        {
            yield return new WaitForSeconds(_Delay);
            _ActionForInvoke.Invoke();
            Destroy(gameObject);
        }
    }



    public class ColorSaverTag : MonoBehaviour
    {
        public Color ColorSaved = Color.white;
    }

    public class ReactionToSomething : MonoBehaviour
    {
        public Dictionary<string, ReactionData> ReactionsDic = new Dictionary<string, ReactionData>();
        private PhysicalBehaviour physicalBehaviour;
        private LimbBehaviour lmb;
        private bool saying = false;

        public static AudioClip lineclip;

        private void Awake()
        {
            lmb = this.gameObject.GetComponent<LimbBehaviour>();
            physicalBehaviour = this.gameObject.GetComponent<PhysicalBehaviour>();
            StartCoroutine(Scan());
        }

        private IEnumerator Scan()
        {
            var cash = new WaitForSeconds(1f);

            while (true)
            {
                yield return cash;

                Collider2D[] thingsInBounds = Physics2D.OverlapCircleAll(this.transform.position, 5f);
                foreach (Collider2D other in thingsInBounds)
                {
                    if (other != null && other.transform.root != this.transform.root && UtilityMethods.CanSay == true && ReactionsDic.ContainsKey(other.gameObject.transform.root.name) && ReactionsDic[other.gameObject.transform.root.name].Triggered == false)
                    {
                        if (lmb != null && lmb.IsConsideredAlive == false || (other.transform.root.GetComponent<PersonBehaviour>() != null && other.transform.root.GetComponent<PersonBehaviour>().IsAlive() == false))
                            continue;

                        StartCoroutine(SayLines(ReactionsDic[other.gameObject.transform.root.name], other.gameObject.transform.root.gameObject));
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (UtilityMethods.CanSay == false && saying == true)
            {
                UtilityMethods.CanSay = true;
            }
        }

        private IEnumerator SayLines(ReactionData reactionData, GameObject obj)
        {
            UtilityMethods.CanSay = false;
            saying = true;
            reactionData.Triggered = true;
            foreach (string line in reactionData.Lines)
            {
                ModAPI.Notify(line);
                physicalBehaviour.MainAudioSource.enabled = true;
                if (lineclip != null)
                    physicalBehaviour.MainAudioSource.PlayOneShot(lineclip, 2.5f);
                yield return new WaitForSeconds(reactionData.delay);
            }

            reactionData.OnEnd?.Invoke(obj);
            UtilityMethods.CanSay = true;
            saying = false;
        }
    }

    public class ReactionData
    {
        public List<string> Lines = new List<string>();
        public float delay = 2f;
        public System.Action<GameObject> OnEnd;
        public bool Triggered = false;
    }

    public class RendererEnabler : MonoBehaviour
    {
        protected SpriteRenderer Renderer;

        // protected bool IsInitialized;
        public void Awake()
        {
            Renderer = this.gameObject.GetComponent<SpriteRenderer>();
        }

        public void OnEnable()
        {
            Renderer.enabled = true;
        }
    }

    public class EyeRandomizer : MonoBehaviour
    {
        public Transform eye;
        private Vector3 _vel = Vector3.zero;
        private Vector3 rand;
        private float timer;

        private void Start()
        {
            eye = this.transform.GetChild(0);
            rand = Random.insideUnitCircle.normalized * Random.Range(0, 0.1f);
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (timer > 2)
            {
                timer = 0f;
                rand = Random.insideUnitCircle.normalized * Random.Range(0, 0.1f);
            }

            eye.transform.localPosition = Vector3.SmoothDamp(eye.transform.localPosition, rand, ref _vel, 0.25f);
        }
    }





    public class RBSegment : MonoBehaviour
    {
        public Collider2D coll;
        public Rigidbody2D rg;
        public RBLine creator;
    }

    public class RBLine : MonoBehaviour
    {
        public int segmentCount;

        public int ropeLayer = 10;

        // 9: Objects
        //10: Debris
        //11: Bounds

        private float lengthMultiplier;
        private float desiredLength;

        public Material InitialRopeMaterial;
        public LineRenderer lineRenderer;
        public float InitialropeDrag = 0f;

        public bool useJointLimits = false;
        public JointAngleLimits2D jointLimits;

        public float segmentDistance;
        public int smoothingFactor = 1;

        public float ropeMassMult;
        public float curveHandleRatio = 0.25f;
        private float _ropeGravityScale = 1f;

        public Vector2 ropeStart;
        public Vector2 ropeEnd;

        public JointConnection[] jointConnections;
        public Rigidbody2D[] ropeSegments;

        public float ropeGravityScale
        {
            get { return _ropeGravityScale; }
            set
            {
                _ropeGravityScale = value;
                if (isInitialized == false)
                    return;
                foreach (var item in ropeSegments)
                {
                    UtilityMethods.DelayedInvoke(0.01f, () =>
                    {
                        item.gravityScale = _ropeGravityScale;
                    });
                }
            }
        }

        public Rigidbody2D RopeStartSegment
        {
            get { return ropeSegments[0]; }
        }

        public Rigidbody2D RopeEndSegment
        {
            get { return ropeSegments[segmentCount - 1]; }
        }

        public float ActualLength
        {
            get { return segmentDistance * (segmentCount - 1); }
        }

        public float DesiredLength
        {
            get { return desiredLength; }
            set
            {
                desiredLength = value;
                if (isInitialized)
                {
                    segmentDistance = DesiredLength / (segmentCount - 1);
                    LengthMultiplier = LengthMultiplier;
                }
            }
        }

        public float ropeWidth;

        public bool isInitialized = false;

        public float LengthMultiplier
        {
            get { return lengthMultiplier; }
            set
            {
                foreach (JointConnection joint in jointConnections)
                {
                    joint.UpdateDistance(value * segmentDistance);
                }
                lengthMultiplier = value;
            }
        }

        public class JointConnection
        {
            public HingeJoint2D hingeJoint;
            public DistanceJoint2D distanceJoint;

            public void UpdateDistance(float distance)
            {
                if (distanceJoint)
                {
                    distanceJoint.distance = distance;
                    return;
                }
                hingeJoint.connectedAnchor = new Vector2(0f, distance);
            }
        }

        public void Awake()
        {
            isInitialized = false;
        }

        // Initialize the rope segments and joints
        public void InitializeRope()
        {
            isInitialized = true;
            segmentDistance = DesiredLength / (segmentCount - 1);
            Vector2 step = (ropeEnd - ropeStart) / (segmentCount - 1);
            Vector2 currentPosition = ropeStart;

            ropeSegments = new Rigidbody2D[segmentCount];
            jointConnections = new JointConnection[segmentCount - 1];

            Rigidbody2D segment = CreateRopeSegment(currentPosition);
            ropeSegments[0] = segment;

            for (int i = 1; i < segmentCount; i++)
            {
                currentPosition += step;
                segment = CreateRopeSegment(currentPosition, segment, out JointConnection joint);
                ropeSegments[i] = segment;
                jointConnections[i - 1] = joint;
            }

            SetupLineRenderer();
        }

        // Update rope visuals
        public void Update()
        {
            UpdateRopeRenderer();
        }

        // Update rope mass based on density and length
        public void FixedUpdate()
        {
            UpdateRopeMass();
        }

        public void UpdateRopeRenderer()
        {
            Vector3[] segmentPositions = new Vector3[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                segmentPositions[i] = ropeSegments[i].transform.position;
            }

            Vector3[] smoothedPositions = SmoothRopeCurve(segmentPositions, smoothingFactor);
            lineRenderer.positionCount = smoothedPositions.Length;
            lineRenderer.SetPositions(smoothedPositions);
        }

        public void UpdateRopeMass()
        {
            float massPerSegment = ropeMassMult * ActualLength / segmentCount;
            foreach (Rigidbody2D segment in ropeSegments)
            {
                segment.mass = massPerSegment;
            }
        }

        // Create a line renderer component for the rope
        private void SetupLineRenderer()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = ropeWidth;
            lineRenderer.endWidth = ropeWidth;
            lineRenderer.material = InitialRopeMaterial;
            lineRenderer.numCornerVertices = 0;
            lineRenderer.positionCount = segmentCount;
        }

        // Create a single rope segment
        public Rigidbody2D CreateRopeSegment(Vector2 position)
        {
            GameObject segmentObject = new GameObject("RB Segment");
            segmentObject.layer = ropeLayer;
            segmentObject.transform.SetParent(transform);
            segmentObject.transform.position = position;

            Rigidbody2D body = segmentObject.AddComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.gravityScale = ropeGravityScale;
            body.drag = InitialropeDrag;

            CircleCollider2D collider = segmentObject.AddComponent<CircleCollider2D>();
            collider.radius = ropeWidth / 2f;

            var segcomp = segmentObject.AddComponent<RBSegment>();
            segcomp.coll = collider;
            segcomp.rg = body;
            segcomp.creator = this;
            return body;
        }

        // Create a rope segment and connect it to the previous segment
        public Rigidbody2D CreateRopeSegment(Vector2 position, Rigidbody2D previousSegment, out JointConnection joint)
        {
            Rigidbody2D body = CreateRopeSegment(position);
            joint = new JointConnection();
            if (useJointLimits)
            {
                joint.hingeJoint = AttachHingeJoint(body, previousSegment, segmentDistance);
                return body;
            }
            joint.distanceJoint = AttachDistanceJoint(body, previousSegment, segmentDistance);
            return body;
        }

        // Create a distance joint between two segments
        public DistanceJoint2D AttachDistanceJoint(Rigidbody2D body1, Rigidbody2D body2, float distance)
        {
            DistanceJoint2D joint = body1.gameObject.AddComponent<DistanceJoint2D>();
            joint.connectedBody = body2;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector2.zero;
            joint.connectedAnchor = Vector2.zero;
            joint.autoConfigureDistance = false;
            joint.distance = distance;
            joint.maxDistanceOnly = false;

            return joint;
        }

        // Create a hinge joint between two segments
        public HingeJoint2D AttachHingeJoint(Rigidbody2D body1, Rigidbody2D body2, float distance)
        {
            HingeJoint2D joint = body1.gameObject.AddComponent<HingeJoint2D>();
            joint.connectedBody = body2;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector2.zero;
            joint.connectedAnchor = new Vector2(0f, distance);
            if (useJointLimits)
            {
                joint.useLimits = useJointLimits;
                joint.limits = jointLimits;
            }
            return joint;
        }

        // Apply smoothing to the rope curve
        public Vector3[] SmoothRopeCurve(Vector3[] positions, int resolution)
        {
            Vector3[] smoothedPositions = new Vector3[(positions.Length) * (1 + resolution) + 1];
            smoothedPositions[0] = positions[0];

            Vector2 point1 = Vector2.zero;
            Vector2 point2 = positions[0];
            Vector2 handle1 = Vector2.zero;
            Vector2 handle2 = Vector2.zero;
            float handleLength = 0f;
            float t = 0f;
            int segmentCount = resolution + 1;
            float delta = 1f / (float)segmentCount;

            for (int i = 0; i < positions.Length; i++)
            {
                point1 = point2;
                point2 = positions[i];

                handleLength = (point2 - point1).magnitude * curveHandleRatio;
                handle1 = point1 + (point2 - (Vector2)positions[Mathf.Max(i - 2, 0)]).normalized * handleLength;
                handle2 = point2 - ((Vector2)positions[Mathf.Min(i + 1, positions.Length - 1)] - point1).normalized * handleLength;

                t = 0f;

                for (int j = 0; j < resolution; j++)
                {
                    t += delta;
                    smoothedPositions[segmentCount * i + j + 1] = CalculateBezierPoint(t, point1, point2, handle1, handle2);
                }
                smoothedPositions[segmentCount * (i + 1)] = point2;
            }

            return smoothedPositions;
        }

        // Calculate a point on a Bezier curve
        public Vector2 CalculateBezierPoint(float t, Vector2 start, Vector2 end, Vector2 handle1, Vector2 handle2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector2 point = uuu * start;           // (1-t)^3 * P0
            point += 3 * uu * t * handle1;         // 3(1-t)^2 * t * P1
            point += 3 * u * tt * handle2;         // 3(1-t) * t^2 * P2
            point += ttt * end;                    // t^3 * P3

            return point;
        }
    }

    public class DeleteWithObject : MonoBehaviour
    {
        public List<GameObject> ToDestroy = new List<GameObject>();

        public void Refresh()
        {
            ToDestroy.RemoveAll(x => !x);
        }

        private void OnDestroy()
        {
            Refresh();
            foreach (var item in ToDestroy)
            {
                Destroy(item);
            }
        }
    }



    public class SmoothMoveObject : MonoBehaviour
    {
        public GameObject obj;
        public float smooth = 0.35f;
        public float RotateSmooth = 0.25f;
        public Vector3 offset;
        private Vector3 _vel = Vector3.zero;
        public bool UseRotation = false;
        public float RotateSpeed = 3;
        public float RotateOffset = 0;
        public bool ShouldUseRotateOffset = false;
        public bool AltPosMode = false;
        public float AltPosMult = 0.3f;
        private float _rotvel;
        public float alrHeihtmult = 0.3f;

        private void LateUpdate()
        {
            if (obj == null)
                Destroy(this);

            //obj.transform.position = Vector3.Lerp(obj.transform.position, transform.position + offset, 1 * Time.deltaTime);

            if (AltPosMode == false)
            {
                obj.transform.position = Vector3.SmoothDamp(obj.transform.position, this.transform.position + offset * this.transform.root.localScale.x, ref _vel, smooth);
            }
            else
            {
                obj.transform.position = Vector3.SmoothDamp(obj.transform.position, (this.transform.position + transform.up * alrHeihtmult) + offset, ref _vel, smooth);
            }

            if (UseRotation == false)
                return;

            float targetAngle = this.transform.eulerAngles.z;

            if (ShouldUseRotateOffset == true)
                targetAngle += RotateOffset * transform.root.localScale.x;

            float angle = Mathf.SmoothDampAngle(obj.transform.eulerAngles.z, targetAngle, ref _rotvel, RotateSmooth);
            obj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    public enum LimbIdFromName
    {
        Head,
        UpperBody,
        MiddleBody,
        LowerBody,
        UpperLegFront,
        LowerLegFront,
        FootFront,
        UpperLeg,
        LowerLeg,
        Foot,
        UpperArmFront,
        LowerArmFront,
        UpperArm,
        LowerArm
    }
    public class LinkedDynamicDecor : DecalableObj
    {
        public DynamicDecoration Linked;



        public override void Shoot(Shot shot)
        {
            if (this.limb != null)
            {
                Color computedColor = limb.CirculationBehaviour.GetComputedColor(limb.GetOriginalBloodType().Color);
                Decal(new DecalInstruction(limb.BloodDecal, shot.point, computedColor, 1f));
            }
        }

        public override void Decal(DecalInstruction instruction)
        {
            if (Linked == null)
            {
                Destroy(this);
                return;
            }



            if (!UserPreferenceManager.Current.Decals)
            {
                return;
            }


            if (Linked.IsActive == false || Linked.IsinAltMod == false)
            {
                return;
            }

            if (!this.decalControllers.Any(e => e.DecalDescriptor == instruction.type))
            {
                DecalControllerBehaviour decalControllerBehaviour = Linked._obj.gameObject.AddComponent<DecalControllerBehaviour>();
                decalControllerBehaviour.DecalDescriptor = instruction.type;
                decalControllerBehaviour.Decal(instruction);
                this.decalControllers.Add(decalControllerBehaviour);
            }
        }

    }

    public class LinkedPhysDecor : DecalableObj
    {
 
        public PhysDecoration linked;
        public override void Decal(DecalInstruction instruction)
        {
            if (linked == null)
            {
                Destroy(this);
                return;
            }

            if (!UserPreferenceManager.Current.Decals)
            {
                return;
            }
            if (!this.decalControllers.Any((DecalControllerBehaviour e) => e.DecalDescriptor == instruction.type))
            {
                DecalControllerBehaviour decalControllerBehaviour = linked._obj.gameObject.AddComponent<DecalControllerBehaviour>();
                decalControllerBehaviour.DecalDescriptor = instruction.type;
                decalControllerBehaviour.Decal(instruction);
                this.decalControllers.Add(decalControllerBehaviour);
            }

        }

    }

    public class LinkedRopeDecor : DecalableObj
    {
        public RopeDecorManager linked;

        public override void Shoot(Shot shot)
        {
            if (this.limb != null)
            {
                Color computedColor = limb.CirculationBehaviour.GetComputedColor(limb.GetOriginalBloodType().Color);
                Decal(new DecalInstruction(limb.BloodDecal, shot.point, computedColor, 1f));
            }
        }

        public override void Decal(DecalInstruction instruction)
        {
            if (linked == null)
            {
                Destroy(this);
                return;
            }

            if (!UserPreferenceManager.Current.Decals)
            {
                return;
            }
            var rand = Random.Range(1, 4);
            for (int i = 0; i < rand; i++)
            {
                var closestClothPieces = linked.clothPieces.PickRandom();

                if(piecedic.ContainsKey(closestClothPieces) == false)
                {
                    var decalcont = new HashSet<DecalControllerBehaviour>();
                    piecedic.Add(closestClothPieces, decalcont);
                }
                piecedic.TryGetValue(closestClothPieces, out var hashset);

                    if (!hashset.Any(e => e.DecalDescriptor == instruction.type))
                    {
                        DecalControllerBehaviour decalControllerBehaviour = closestClothPieces.gameObject.AddComponent<DecalControllerBehaviour>();
                        decalControllerBehaviour.DecalDescriptor = instruction.type;
                        decalControllerBehaviour.Decal(instruction);
                    hashset.Add(decalControllerBehaviour);
                    }
                
            }
        }
        Dictionary<Rigidbody2D, HashSet<DecalControllerBehaviour>> piecedic = new Dictionary<Rigidbody2D, HashSet<DecalControllerBehaviour>>();
    }

    public class DecalableObj : MonoBehaviour
    {
        public readonly HashSet<DecalControllerBehaviour> decalControllers = new HashSet<DecalControllerBehaviour>();
        public float cleantimer;
        public float cleanuptime = 35;
        public LimbBehaviour limb;

        public void Awake()
        {
            limb = this.GetComponent<LimbBehaviour>();
        }
        public virtual void Shoot(Shot shot)
        {
            if(this.limb != null)
            {
                Color computedColor = limb.CirculationBehaviour.GetComputedColor(limb.GetOriginalBloodType().Color);
                Decal(new DecalInstruction(limb.BloodDecal, shot.point, computedColor, 1f));
            }
        }

        public virtual void Decal(DecalInstruction instruction)
        {
            if (!UserPreferenceManager.Current.Decals)
            {
                return;
            }
            if (!this.decalControllers.Any((DecalControllerBehaviour e) => e.DecalDescriptor == instruction.type))
            {
                DecalControllerBehaviour decalControllerBehaviour = gameObject.AddComponent<DecalControllerBehaviour>();
                decalControllerBehaviour.DecalDescriptor = instruction.type;
                decalControllerBehaviour.Decal(instruction);
                this.decalControllers.Add(decalControllerBehaviour);
            }
        }
        void Update()
        {
            cleantimer += Time.deltaTime;
            if(this.cleantimer > cleanuptime)
            {
                cleantimer = 0;
                if (this.decalControllers != null && this.decalControllers.Any<DecalControllerBehaviour>())
                {
                    decalControllers.ElementAt(Random.Range(0, decalControllers.Count)).Clear();
                }
            }

        }
    }

    public static class TextureAutoSlicerRuntime
    {
        public static Dictionary<Texture2D, List<Sprite>> CachedText = new Dictionary<Texture2D, List<Sprite>>();

        /// <summary>
        /// Automatically slices a texture into sprites by detecting non-transparent regions at runtime.
        /// </summary>
        /// <param name="texture">The Texture2D to slice (must be readable).</param>
        /// <param name="alphaTolerance">The alpha tolerance to consider a pixel as transparent (0-1).</param>
        /// <param name="minSpriteSize">Minimum size of sprites to detect (in pixels).</param>
        /// <param name="padding">Padding around each sprite (in pixels).</param>
        /// <param name="pixelsPerUnit">Pixels per unit for the created sprites.</param>
        /// <returns>A list of Sprites created from the texture.</returns>
        public static List<Sprite> AutoSliceTexture(
            Texture2D texture,
            float pixelsPerUnit = 35.0f,
            float alphaTolerance = 0.1f,
            int minSpriteSize = 4,
            int padding = 0
            )
        {
            if (texture == null)
            {
                Debug.LogError("Texture is null.");
                return null;
            }

            // Ensure the texture is readable
            if (!texture.isReadable)
            {
                Debug.LogError("Texture is not readable. Please set the texture's Read/Write Enabled flag in the import settings.");
                return null;
            }

            if (CachedText.TryGetValue(texture, out var list))
            {
                return list;
            }

            Color[] pixels = texture.GetPixels();
            int width = texture.width;
            int height = texture.height;

            // Keep track of visited pixels
            bool[] visited = new bool[pixels.Length];

            List<Sprite> sprites = new List<Sprite>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;

                    if (visited[index])
                        continue;

                    if (pixels[index].a > alphaTolerance)
                    {
                        // Start a new sprite detection
                        Rect spriteRect = GetSpriteRect(pixels, visited, width, height, x, y, alphaTolerance);

                        // Check if the sprite size meets the minimum size requirement
                        if (spriteRect.width >= minSpriteSize && spriteRect.height >= minSpriteSize)
                        {
                            // Apply padding
                            spriteRect.xMin = Mathf.Max(0, spriteRect.xMin - padding);
                            spriteRect.yMin = Mathf.Max(0, spriteRect.yMin - padding);
                            spriteRect.xMax = Mathf.Min(width, spriteRect.xMax + padding);
                            spriteRect.yMax = Mathf.Min(height, spriteRect.yMax + padding);

                            // Create a new sprite from the defined rect with the specified pixels per unit
                            Sprite sprite = Sprite.Create(texture, Rect(spriteRect), new Vector2(0.5f, 0.5f), pixelsPerUnit);

                            sprites.Add(sprite);
                        }
                    }
                    else
                    {
                        visited[index] = true;
                    }
                }
            }

            Debug.Log("Automatic texture slicing completed successfully. Slices created: " + sprites.Count);
            CachedText.Add(texture, sprites);
            return sprites;
        }

        private static Rect GetSpriteRect(Color[] pixels, bool[] visited, int width, int height, int startX, int startY, float alphaTolerance)
        {
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startX, startY));

            int xMin = startX;
            int xMax = startX;
            int yMin = startY;
            int yMax = startY;

            while (queue.Count > 0)
            {
                Vector2Int pixel = queue.Dequeue();
                int x = pixel.x;
                int y = pixel.y;

                if (x < 0 || x >= width || y < 0 || y >= height)
                    continue;

                int index = y * width + x;

                if (visited[index])
                    continue;

                if (pixels[index].a <= alphaTolerance)
                {
                    visited[index] = true;
                    continue;
                }

                visited[index] = true;

                // Update bounds
                xMin = Mathf.Min(xMin, x);
                xMax = Mathf.Max(xMax, x);
                yMin = Mathf.Min(yMin, y);
                yMax = Mathf.Max(yMax, y);

                // Enqueue neighboring pixels
                queue.Enqueue(new Vector2Int(x + 1, y));
                queue.Enqueue(new Vector2Int(x - 1, y));
                queue.Enqueue(new Vector2Int(x, y + 1));
                queue.Enqueue(new Vector2Int(x, y - 1));
            }

            // Convert to Rect
            Rect rect = new Rect(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
            return rect;
        }


        private static Dictionary<string, List<Sprite>> spriteCache = new Dictionary<string, List<Sprite>>();

        /// <summary>
        /// Slices a texture into sprites of specified width and height, with caching.
        /// </summary>
        /// <param name="texture">The texture to slice.</param>
        /// <param name="sliceWidth">The width of each slice.</param>
        /// <param name="sliceHeight">The height of each slice.</param>
        /// <param name="pixelsPerUnit">Pixels per unit for the sprites.</param>
        /// <returns>A list of sprites generated from the texture.</returns>
        public static List<Sprite> ResolutionSliceTexture(Texture2D texture, int sliceWidth = 12, int sliceHeight = 12, float pixelsPerUnit = 35)
        {
            Vector2 pivot = new Vector2(0.5f, 0.5f);

            if (texture == null)
            {
                Debug.LogError("Texture is null.");
                return new List<Sprite>();
            }

            if (sliceWidth <= 0 || sliceHeight <= 0)
            {
                Debug.LogError("Slice width and height must be positive integers.");
                return new List<Sprite>();
            }

            // Generate a unique cache key based on texture and parameters
            string cacheKey = GenerateCacheKey(texture, sliceWidth, sliceHeight, pixelsPerUnit);

            // Check if the sprites are already in the cache
            if (spriteCache.ContainsKey(cacheKey))
            {
                return spriteCache[cacheKey];
            }

            List<Sprite> sprites = new List<Sprite>();
            int textureWidth = texture.width;
            int textureHeight = texture.height;

            // Calculate the number of slices needed in each direction
            int slicesX = Mathf.CeilToInt((float)textureWidth / sliceWidth);
            int slicesY = Mathf.CeilToInt((float)textureHeight / sliceHeight);

            // Loop over the texture to create sprites
            for (int y = 0; y < slicesY; y++)
            {
                int yPos = textureHeight - (y + 1) * sliceHeight;
                int currentSliceHeight = sliceHeight;

                // Adjust for edges
                if (yPos < 0)
                {
                    currentSliceHeight += yPos;
                    yPos = 0;
                }
                if (yPos + currentSliceHeight > textureHeight)
                {
                    currentSliceHeight = textureHeight - yPos;
                }

                for (int x = 0; x < slicesX; x++)
                {
                    int xPos = x * sliceWidth;
                    int currentSliceWidth = sliceWidth;

                    if (xPos + currentSliceWidth > textureWidth)
                    {
                        currentSliceWidth = textureWidth - xPos;
                    }

                    // Create a new Texture2D for the slice
                    Texture2D slice = new Texture2D(currentSliceWidth, currentSliceHeight);
                    slice.SetPixels(texture.GetPixels(xPos, yPos, currentSliceWidth, currentSliceHeight));
                    slice.filterMode = FilterMode.Point;
                    slice.Apply();

                    // Create a sprite from the slice
                    Sprite sprite = Sprite.Create(slice, new Rect(0, 0, currentSliceWidth, currentSliceHeight), pivot, pixelsPerUnit);
                    sprites.Add(sprite);
                }
            }

            // Store the generated sprites in the cache
            spriteCache[cacheKey] = sprites;

            return sprites;
        }

        /// <summary>
        /// Generates a unique cache key based on texture and parameters.
        /// </summary>
        /// <param name="texture">The texture being sliced.</param>
        /// <param name="sliceWidth">Slice width.</param>
        /// <param name="sliceHeight">Slice height.</param>
        /// <param name="pixelsPerUnit">Pixels per unit.</param>
        /// <returns>A unique string key for caching.</returns>
        private static string GenerateCacheKey(Texture2D texture, int sliceWidth, int sliceHeight, float pixelsPerUnit)
        {
            int textureID = texture.GetInstanceID();
            string key = $"{textureID}_{sliceWidth}_{sliceHeight}_{pixelsPerUnit}";
            return key;
        }


        private static Rect Rect(Rect rect)
        {
            int x = Mathf.RoundToInt(rect.x);
            int y = Mathf.RoundToInt(rect.y);
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);
            return new Rect(x, y, width, height);
        }
    }



    public class ShakeUiNotif : MonoBehaviour
    {
        public float shakeDuration = 5;
        public float shakeAmount = 0.5f;
        private RectTransform rect;
        private Vector3 originalPosition;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();


            StartCoroutine(Shake());
        }



        private IEnumerator Shake()
        {
            yield return null;
            float elapsed = 0f;
            originalPosition = rect.anchoredPosition;
            while (elapsed < shakeDuration)
            {
                if (originalPosition.y < originalPosition.y + 1)
                {
                    originalPosition = new Vector2(originalPosition.x, rect.anchoredPosition.y);
                }

                Vector2 randomOffset = Random.insideUnitCircle * shakeAmount;
                rect.anchoredPosition = originalPosition + (Vector3)randomOffset;

                elapsed += Time.deltaTime;
                yield return null;
            }
            rect.anchoredPosition = originalPosition;

        }
    }


    public class DelayerFrameCaller : MonoBehaviour
    {
        private Action _ActionForInvoke;

        public void Init(Action actionForInvoke)
        {
            _ActionForInvoke = actionForInvoke;
        }

        private void Start()
        {
            StartCoroutine(Delayer());
        }

        private IEnumerator Delayer()
        {
            yield return null;
            _ActionForInvoke.Invoke();
            Destroy(gameObject);
        }
    }

    public static class JointController
    {
        public static void RemakeConnectedAnchor(GameObject obj, Vector2 Canchor)
        {
            UtilityMethods.DelayedInvoke(0.05f, () =>
            {
                if (obj.TryGetComponent<HingeJoint2D>(out var hj))
                {
                    hj.connectedAnchor = Canchor;
                }
            });


        }
    }



    public class TimeSlowerManager
    {
        public static Dictionary<PersonBehaviour, float> ActiveUsers = new Dictionary<PersonBehaviour, float>();

        // Maximum allowed speed scale (highest possible slow factor)
        public static float MaxSpeedScale = 20f;

        private static float debugval = 1;



        [HarmonyPatch]
        class DeltaTimePatches
        {



            public static float PatchMult = 1.0f;

            static Assembly TargetAssembly()
            {
                // Adjust if necessary:
                return AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
            }

            static IEnumerable<MethodBase> TargetMethods()
            {
                var assembly = TargetAssembly();
                if (assembly == null)
                    yield break;


                // Debug.Log(typeof(LimbBehaviour).Namespace);
                // Debug.Log(typeof(LimbBehaviour).Assembly.GetName().Name);
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                {
                    // Only patch MonoBehaviour-derived types...
                    if (!typeof(MonoBehaviour).IsAssignableFrom(type))
                        continue;

                   // if (!typeof(MonoBehaviour).IsAssignableFrom(type))
                     //   continue;

                    // ... that are NOT in the UnityEngine namespace.
                    // Adjust this condition if your scripts have a certain namespace pattern.
                    if (type.Namespace != null)
                        continue;


                    //In case something will broke
                    // if (Ignoretypes.Contains(type))
                    //  continue;

                    // Debug.Log(type.Name);
                    MethodInfo[] methods;
                    try
                    {
                        methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    }
                    catch (NotSupportedException)
                    {
                        continue;
                    }

                    foreach (var method in methods)
                    {

                        // Skip abstract or no-body methods
                        if (method.IsAbstract || method.IsVirtual || method.GetMethodBody() == null || method.GetBaseDefinition() != method)
                            continue;



                        if (method.IsAssembly) // indicates internal
                            continue;

                        if (method.ReturnType.IsPointer || method.GetParameters().Any(p => p.ParameterType.IsPointer))
                            continue;

                        // Ensure the method is from Assembly-CSharp
                        if (method.DeclaringType.Assembly != assembly)
                            continue;

                        //LOG SUFFERING AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
                        // Debug.Log(method.Name + " " + method.DeclaringType.Name);
                        
                        yield return method;
                    }
                }
            }
            //In case something will broke, I HOPE NOT!!!!
            //static HashSet<Type> Ignoretypes = new HashSet<Type>();

            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = new List<CodeInstruction>(instructions);

                for (int i = 0; i < code.Count; i++)
                {
                    var instr = code[i];
                    if ((instr.opcode == OpCodes.Call || instr.opcode == OpCodes.Callvirt) && instr.operand is MethodInfo mi)
                    {
                        if (mi.DeclaringType == typeof(Time) && mi.Name == "get_deltaTime")
                        {
                            code.InsertRange(i + 1, new[]
                            {
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(DeltaTimePatches), nameof(PatchMult))),
                       // new CodeInstruction(OpCodes.Ldsfld, TimeSlowerManager.CurrentSpeedScale), ERROR FOR SOME REASON AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABBBAAAAA AA ABAAA
                        new CodeInstruction(OpCodes.Div)
                    });
                            i += 2;
                        }
                    }
                }

                return code;
            }
        }


        //[HarmonyPatch(typeof(Global), "UpdatePitch")]
        //public static class globalpatch
        //{
        //    [HarmonyPrefix]
        //    public static bool Prefix(Global __instance, ref float timeScale)
        //    {
            
        //        timeScale /= CurrentSpeedScale;
       
        //        return true;
        //    }
        //}


        #region OldPatches
        // OLD INDIVIDUAL PATCHES

        //[HarmonyPatch(typeof(BallisticsEmitter), "BallisticIteration")]
        //public static class BallisticsEmitter_BallisticIteration_Transpiler
        //{
        //    [HarmonyTranspiler]
        //    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //    {
        //        var getDeltaTime = AccessTools.PropertyGetter(typeof(UnityEngine.Time), nameof(UnityEngine.Time.deltaTime));
        //        var getScaledDeltaTime = AccessTools.PropertyGetter(typeof(TimeSlowerManager), "ScaledDeltaTime");

        //        foreach (var instruction in instructions)
        //        {
        //            if (instruction.opcode == OpCodes.Call && Equals(instruction.operand, getDeltaTime))
        //            {
        //                yield return new CodeInstruction(OpCodes.Call, getScaledDeltaTime);
        //            }
        //            else
        //            {
        //                yield return instruction;
        //            }
        //        }
        //    }
        //}


        //[HarmonyPatch(typeof(BlasterBehaviour), "Shoot")]
        //public static class BlasterBehaviour_Shoot_PrefixPatch
        //{
        //    [HarmonyPrefix]
        //    public static bool Prefix(BlasterBehaviour __instance)
        //    {

        //        if(CurrentSpeedScale < 1.1)
        //        {
        //            return true;
        //        }

        //        Vector2 vector = __instance.BarrelPosition;
        //        Vector2 a = __instance.BarrelDirection;
        //        __instance.SetField("t",0f);
        //        __instance.GetComponent<PhysicalBehaviour>().rigidbody.AddForceAtPosition(a * -__instance.Recoil, vector, ForceMode2D.Impulse);
        //        var bolt = UnityEngine.Object.Instantiate<GameObject>(__instance.Bolt, vector, Quaternion.identity);
        //            bolt.transform.right = a + __instance.InaccuracyMultiplier * UnityEngine.Random.insideUnitCircle;
        //        bolt.GetComponent<BlasterboltBehaviour>().Speed /= CurrentSpeedScale;
        //        __instance.blasterSoundSource.PlayOneShot((__instance.Clips != null && __instance.Clips.Length != 0) ? __instance.Clips.PickRandom<AudioClip>() : __instance.blasterSoundSource.clip);
        //        CameraShakeBehaviour.main.Shake(0.5f * __instance.ScreenShakeMultiplier, vector, 1f);
        //        __instance.Muzzleflash.Play();


        //        return false;
        //    }
        //}



        //[HarmonyPatch(typeof(BlasterboltBehaviour), "Update")]
        //public static class BlasterboltBehaviourPrefixPatch
        //{
        //    public static Dictionary<BlasterboltBehaviour,float> InitialSpeed = new Dictionary<BlasterboltBehaviour, float>();
        //    [HarmonyPrefix]
        //    public static void PostFix(BlasterboltBehaviour __instance)
        //    {
        //        if(CurrentSpeedScale > 1.1)
        //        {
        //            if(InitialSpeed.ContainsKey(__instance) == false)
        //            {
        //                InitialSpeed.Add(__instance, __instance.Speed);
        //            }
        //            __instance.Speed = InitialSpeed[__instance] / CurrentSpeedScale;
        //        }

        //    }
        //}

        #endregion
        public static float CalculateAppliedScale(Transform Obj)
        {
            var globalScale = TimeSlowerManager.CurrentSpeedScale;
            var appliedScale = globalScale;

            PersonRecorder.PersonsRoots.TryGetValue(Obj.transform.root, out var Person);

            if(Person == null)
            {
                return globalScale;
            }

            // Check if this person is one of the active users (i.e., their root GameObject is a key in ActiveUsers)
            if (TimeSlowerManager.ActiveUsers.TryGetValue(Person, out float mySlowValue))
            {
                // This person is actively slowing time, so adjust their scale
                // This ratio ensures that if their slow value matches the global max,
                // they'll be unaffected (scale=1). If it's lower, they'll be partially affected.
                float maxSlow = TimeSlowerManager.GetMaxSlow;
                if (mySlowValue > 0f)
                {
                    appliedScale = maxSlow / mySlowValue;
                }
                else
                {
                    // In case of a zero slow value (shouldn't happen), just default to 1 (no effect)
                    appliedScale = 1f;
                }
            }
            else
            {
                // Person is not an active user, just use the global scale
                appliedScale = globalScale;
            }

            return appliedScale;
        }



        // Returns the maximum slow factor from all active users. If none are active, returns 1.
        public static float GetMaxSlow
        {
            get
            {
                return ((ActiveUsers == null || ActiveUsers.Count == 0) ? 1f : ActiveUsers.Values.Max());
            }
        }

        // Scaled delta time for use in time-managed behaviors.
        public static float ScaledDeltaTime
        {
            get
            {
                return Time.deltaTime * GetMaxSlow;
            }
        }

        // Current speed scale is simply the maximum slow factor.
        public static float CurrentSpeedScale
        {
            get
            {
                DeltaTimePatches.PatchMult = GetMaxSlow;
                return GetMaxSlow;
            }
        }

        public static HashSet<TimeManaged> ScaledObjects = new HashSet<TimeManaged>();

        /// <summary>
        /// Activates time slow for a particular user. If the user is already active, updates their slow value.
        /// </summary>
        public static void ActivateTimeSlow(PersonBehaviour gameObj, float slowValue)
        {
            slowValue = Mathf.Clamp(slowValue, 0.01f, MaxSpeedScale);

            if (!ActiveUsers.ContainsKey(gameObj))
            {
                ActiveUsers.Add(gameObj, slowValue);
            }
            else
            {
                ActiveUsers[gameObj] = slowValue;
            }

            // Update all scaled objects since a new slow factor might have changed the maximum
            ToggleSlowTime();
        }

        /// <summary>
        /// Deactivates time slow for a particular user, removing them from the active users.
        /// </summary>
        public static void DeactivateTimeSlow(PersonBehaviour gameObj)
        {
            if (ActiveUsers.ContainsKey(gameObj))
            {
                ActiveUsers.Remove(gameObj);
                ToggleSlowTime();
            }
        }

        /// <summary>
        /// Updates the time scaling on all registered time-managed objects.
        /// </summary>
        public static void ToggleSlowTime()
        {
            foreach (TimeManaged scaledObject in ScaledObjects)
            {
                if (scaledObject != null) // Ensure object wasn't destroyed
                {
                    scaledObject.UpdateTimeScale();
                }
            }
        }


        [Obsolete]
        public void SwitchTime(ITimeStopUser caller)
        {
            if (TimeSlowerManager.CurrentUser == caller)
            {
                TimeSlowerManager.CurrentUser = null;
                TimeSlowerManager.ToggleSlowTime();
            }
            else if (MaxSpeedScale > TimeSlowerManager.CurrentSpeedScale)
            {

                TimeSlowerManager.CurrentUser = caller;
                TimeSlowerManager.ToggleSlowTime();
            }
        }

        [Obsolete]
        public interface ITimeStopUser
        {
            float SelectedSpeedScale { get; set; }
            GameObject thisGameobject { get; set; }
        }

        [Obsolete]
        public static ITimeStopUser CurrentUser;


        [Obsolete]
        public static Dictionary<GameObject, ITimeStopUser> PassiveUsers = new Dictionary<GameObject, ITimeStopUser>();

    }

    [SkipSerialisation]
    public abstract class TimeManaged : MonoBehaviour
    {
      


        public void Start()
        {
            TimeSlowerManager.ScaledObjects.Add(this);
            AfterAwake();
            UpdateTimeScale();
        }

        public void OnDestroy()
        {
            TimeSlowerManager.ScaledObjects.Remove(this);
        }

        public abstract void UpdateTimeScale();
        public abstract void AfterAwake();
    }

    [SkipSerialisation]
    public class GunTimeManaged : TimeManaged
    {
        public FirearmBehaviour Firearm;
        private float? OriginalFireRate = null;


        public override void AfterAwake()
        {
            DelayedInvoke(0.1f, () =>
            {
                if (Firearm != null)
                {
                    foreach (var item in gameObject.GetComponentsInChildren<ParticleSystem>())
                    {
                        item.gameObject.GetOrAddComponent<ParticlesTimeManaged>().ParticleSystemComponent = item;
                    }
                }
            });
        }

        public override void UpdateTimeScale()
        {
         //   if (OriginalFireRate == null)
        //    {
          //      OriginalFireRate = Firearm.AutomaticFireInterval;
               
          //  }


          
            //Firearm.AutomaticFireInterval = (float)OriginalFireRate * TimeSlowerManager.CurrentSpeedScale;
        }
    }



    [SkipSerialisation]
    public class PBTimeManaged : TimeManaged
    {
        public PhysicalBehaviour pb;
        private float? OriginalGravityScale = null;
        private float? OriginalAng = null;
        private float? OriginalDrag = null;
        public override void AfterAwake()
        {
            
        }

        public override void UpdateTimeScale()
        {
            if (OriginalGravityScale == null)
            {
                OriginalGravityScale = pb.rigidbody.gravityScale;
                OriginalAng = pb.rigidbody.angularDrag;
                OriginalDrag = pb.rigidbody.drag;
            }

            // Apply global scaling based on GetMaxSlow
            float scale = TimeSlowerManager.CalculateAppliedScale(this.transform);

            pb.rigidbody.gravityScale = Mathf.Clamp((float)OriginalGravityScale / scale,0.01f,999);
            pb.rigidbody.angularDrag = (float)OriginalAng * scale;
            pb.rigidbody.drag = ((float)OriginalDrag * scale * 0.4f);
        }
    }

    [SkipSerialisation]
    public class ParticlesTimeManaged : TimeManaged
    {
        public ParticleSystem ParticleSystemComponent;
        private float OriginalPlaybackSpeed;

        public override void AfterAwake()
        {
            OriginalPlaybackSpeed = ParticleSystemComponent.main.simulationSpeed;
        }

        public override void UpdateTimeScale()
        {
            if (ParticleSystemComponent == null)
                return;



            float scale = TimeSlowerManager.CurrentSpeedScale;
            var main = ParticleSystemComponent.main;


            main.simulationSpeed = OriginalPlaybackSpeed / scale;
        }
    }

    [SkipSerialisation]
    public class HumansTimeManaged : TimeManaged
    {
        public PersonBehaviour Person;
        private float OriginalStrength;
        private float OriginalImpactDamageMultiplier;

        public override void AfterAwake()
        {
            OriginalStrength = Person.Limbs[0].BaseStrength;
            OriginalImpactDamageMultiplier = Person.Limbs[0].ImpactDamageMultiplier;
        }

        public override void UpdateTimeScale()
        {
            var appliedScale = TimeSlowerManager.CalculateAppliedScale(this.transform);
            Debug.Log(appliedScale);

            // Update ragdoll animations
            foreach (RagdollPose poseState in Person.LinkedPoses.Values)
            {
                poseState.AnimationSpeedMultiplier = -1f / appliedScale;
            }

            // Update limb properties
            foreach (LimbBehaviour limb in Person.Limbs)
            {
                // Similar to before, but using appliedScale
                limb.BaseStrength = OriginalStrength / appliedScale;
                limb.ImpactDamageMultiplier = OriginalImpactDamageMultiplier * appliedScale;
            }
        }

       
    }

}
