using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Utility.UtilityMethods;

namespace ENSYS
{
    public static class RegrowthModuleCache
    {
        private static List<(PersonBehaviour, LimbInformation[])> m_cached = new List<(PersonBehaviour, LimbInformation[])>();

        public static List<(PersonBehaviour, LimbInformation[])> Cached
        {
            get
            {
                m_cached.RemoveAll(c => c.Item1 == null);
                return new List<(PersonBehaviour, LimbInformation[])>(m_cached);
            }
        }

        public static void Cache()
        {
            ModAPI.OnItemSpawned += ModAPI_OnItemSpawned;
        }

        private static void ModAPI_OnItemSpawned(object sender, UserSpawnEventArgs e)
        {
            if (e.Instance.TryGetComponent(out PersonBehaviour personBehaviour))
            {
                CachePersonBehaviour(personBehaviour);
            }
        }

        private static void CachePersonBehaviour(PersonBehaviour personBehaviour)
        {
            // Avoid caching the same person more than once
            if (IsCached(personBehaviour))
                return;

            if(personBehaviour.TryGetComponent<BeingSliced>(out var sl))
            {
                personBehaviour.StartCoroutine(CollectWithDelay(personBehaviour));
                return;
            }

            var limbInformations = personBehaviour.Limbs.Select(l => new LimbInformation(l)).ToArray();
            m_cached.Add((personBehaviour, limbInformations));
        }

        private static IEnumerator CollectWithDelay(PersonBehaviour person)
        {
            yield return new WaitForSeconds(0.15f);

            var limbInformations = person.Limbs.Select(l => new LimbInformation(l)).ToArray();
            m_cached.Add((person, limbInformations));
        }

        // Check if the person is already cached
        public static bool IsCached(PersonBehaviour personBehaviour)
        {
            return m_cached.Any(c => c.Item1 == personBehaviour);
        }

        // Get the limb information for a cached person
        public static LimbInformation[] GetLimbInformations(PersonBehaviour personBehaviour)
        {
            return m_cached.First(c => c.Item1 == personBehaviour).Item2;
        }
    }

    public struct LimbInformations
    {
        public LimbInformation[] list;
    }

    public struct LimbInformation
    {
        public LimbInformation(LimbBehaviour limbBehaviour)
        {
            name = limbBehaviour.name;
            transformPath = UtilityCoreMethods.GetHierarchyPath(limbBehaviour.Person.transform, limbBehaviour.transform);
            if (limbBehaviour.transform.parent != null)
            {
                parentTransformPath = UtilityCoreMethods.GetHierarchyPath(limbBehaviour.transform.root, limbBehaviour.transform.parent);
            }
            else
            {
                parentTransformPath = "";
            }
            connectedLimbsPath = new string[limbBehaviour.ConnectedLimbs.Count];
            for (int i = 0; i < limbBehaviour.ConnectedLimbs.Count; i++)
            {
                connectedLimbsPath[i] = UtilityCoreMethods.GetHierarchyPath(limbBehaviour.ConnectedLimbs[i].transform.root, limbBehaviour.ConnectedLimbs[i].transform);
            }
            hingeJointInformation = new HingeJointInformation(limbBehaviour);
            orderInPerson = limbBehaviour.Person.Limbs.ToList().IndexOf(limbBehaviour);
            pushToLimbsPath = new string[limbBehaviour.CirculationBehaviour.PushesTo.Length];
            for (int i = 0; i < limbBehaviour.CirculationBehaviour.PushesTo.Length; i++)
            {
                pushToLimbsPath[i] = UtilityCoreMethods.GetHierarchyPath(limbBehaviour.CirculationBehaviour.PushesTo[i].transform.root, limbBehaviour.CirculationBehaviour.PushesTo[i].transform);
            }

            if (limbBehaviour.CirculationBehaviour.Source != null)
            {
                sourcePath = UtilityCoreMethods.GetHierarchyPath(limbBehaviour.CirculationBehaviour.Source.transform.root, limbBehaviour.CirculationBehaviour.Source.transform);
                indexInSource = limbBehaviour.CirculationBehaviour.Source.PushesTo.ToList().IndexOf(limbBehaviour.CirculationBehaviour);
            }
            else
            {
                sourcePath = "";
                indexInSource = 0;
            }
            localScale = limbBehaviour.transform.localScale;
            nodeInformation = new NodeInformation(limbBehaviour);
            if (limbBehaviour.NearestLimbToBrain != null)
            {
                nearestLimbToBrainPath = UtilityCoreMethods.GetHierarchyPath(limbBehaviour.NearestLimbToBrain.transform.root, limbBehaviour.NearestLimbToBrain.transform);
            }
            else
            {
                nearestLimbToBrainPath = "";
            }
            try
            {
                indexInSerialiseInstructions = limbBehaviour.Person.gameObject.GetComponent<SerialiseInstructions>().RelevantTransforms.ToList().IndexOf(limbBehaviour.transform);
            }
            catch
            {
                indexInSerialiseInstructions = -1;
            }
            posesInformation = new PoseInformation[limbBehaviour.Person.Poses.Count];
            for (int i = 0; i < limbBehaviour.Person.Poses.Count; i++)
            {
                posesInformation[i] = new PoseInformation(limbBehaviour.Person.Poses[i], limbBehaviour);
            }
            originalJointLimits = limbBehaviour.OriginalJointLimits;
            adjacentLimbs = new string[limbBehaviour.SkinMaterialHandler.adjacentLimbs.Length];
            for (int i = 0; i < adjacentLimbs.Length; i++)
            {
                adjacentLimbs[i] = UtilityCoreMethods.GetHierarchyPath(limbBehaviour.SkinMaterialHandler.adjacentLimbs[i].transform.root, limbBehaviour.SkinMaterialHandler.adjacentLimbs[i].transform);
            }
            distanceBrain = limbBehaviour.DistanceToBrain;
            if (limbBehaviour.TryGetComponent<ShatteredObjectGenerator>(out ShatteredObjectGenerator shatteredObjectGenerator) && shatteredObjectGenerator.ConnectTo != null)
            {
                shatteredConnectedTo = UtilityCoreMethods.GetHierarchyPath(shatteredObjectGenerator.ConnectTo.transform.root, shatteredObjectGenerator.ConnectTo.transform);
            }
            else
            {
                shatteredConnectedTo = "";
            }
            try
            {
                LimbBehaviour shatteredParentLimb = limbBehaviour.Person.Limbs.Where(limb => limb.GetComponent<ShatteredObjectGenerator>().ConnectTo == limbBehaviour.PhysicalBehaviour.rigidbody).FirstOrDefault();
                shatteredParent = UtilityCoreMethods.GetHierarchyPath(shatteredParentLimb.transform.root, shatteredParentLimb.transform);
            }
            catch
            {
                shatteredParent = "";
            }
            IEnumerable<LimbBehaviour> allGoreStrings = limbBehaviour.Person.Limbs.Where(limb => limb.GetComponent<GoreStringBehaviour>() != null);
            LimbBehaviour[] limbGoreStrings = allGoreStrings.Where(goreString => goreString.GetComponent<GoreStringBehaviour>().Other == limbBehaviour.PhysicalBehaviour.rigidbody).ToArray();
            goreStringsPaths = new string[limbGoreStrings.Count()];
            for (int i = 0; i < limbGoreStrings.Count(); i++)
            {
                goreStringsPaths[i] = UtilityCoreMethods.GetHierarchyPath(limbGoreStrings[i].transform.root, limbGoreStrings[i].transform);
            }
            SpriteRenderer limbSpriteRenderer = limbBehaviour.PhysicalBehaviour.spriteRenderer;
            Material limbMaterial = limbSpriteRenderer.material;
            sortingLayerName = limbSpriteRenderer.sortingLayerName;
            sortingOrder = limbSpriteRenderer.sortingOrder;
            isZombie = limbBehaviour.IsZombie;
            breakingThresold = limbBehaviour.BreakingThreshold;
            MotorStrength = limbBehaviour.MotorStrength;
        }

        public string name;
        public string transformPath;
        public string parentTransformPath;

        //circulation
        public string[] connectedLimbsPath;

        public string[] pushToLimbsPath;
        public string sourcePath;
        public int indexInSource;

        //
        public string nearestLimbToBrainPath;

        public int orderInPerson;
        public Vector3 localScale;
        public Vector2 originalJointLimits;
        public HingeJointInformation hingeJointInformation;
        public NodeInformation nodeInformation;
        public PoseInformation[] posesInformation;
        public int indexInSerialiseInstructions;
        public string[] adjacentLimbs;
        public int distanceBrain;
        public float breakingThresold;

        //
        public string shatteredConnectedTo;

        public string shatteredParent;

        //
        public string[] goreStringsPaths;

        public string sortingLayerName;
        public int sortingOrder;
        public bool isZombie;

        public float MotorStrength;
    }

    public struct PoseInformation
    {
        public PoseInformation(RagdollPose ragdollPose, LimbBehaviour limbBehaviour)
        {
            poseIndexInPerson = limbBehaviour.Person.Poses.IndexOf(ragdollPose);
            if (ragdollPose.Angles.Select(angle => angle.Limb).Contains(limbBehaviour))
            {
                RagdollPose.LimbPose limbPose = ragdollPose.Angles.Where(angle => angle.Limb == limbBehaviour).FirstOrDefault();
                limbIndexInPose = ragdollPose.Angles.IndexOf(limbPose);
            }
            else
            {
                limbIndexInPose = -1;
            }
        }

        public int poseIndexInPerson;
        public int limbIndexInPose;
    }

    public struct NodeInformation
    {
        public NodeInformation(LimbBehaviour limbBehaviour)
        {
            connectionsTransformPaths = new string[limbBehaviour.NodeBehaviour.Connections.Length];
            indexInConnectetions = new Dictionary<string, int>();
            for (int i = 0; i < limbBehaviour.NodeBehaviour.Connections.Length; i++)
            {
                connectionsTransformPaths[i] = UtilityCoreMethods.GetHierarchyPath(limbBehaviour.NodeBehaviour.Connections[i].transform.root, limbBehaviour.NodeBehaviour.Connections[i].transform);
            }
            foreach (ConnectedNodeBehaviour connectedNode in limbBehaviour.NodeBehaviour.Connections)
            {
                indexInConnectetions.Add(UtilityCoreMethods.GetHierarchyPath(connectedNode.transform.root, connectedNode.transform), connectedNode.Connections.ToList().IndexOf(limbBehaviour.NodeBehaviour));
            }
            isRoot = limbBehaviour.NodeBehaviour.IsRoot;
            value = limbBehaviour.NodeBehaviour.Value;
        }

        public string[] connectionsTransformPaths;
        public bool isRoot;
        public int value;
        public Dictionary<string, int> indexInConnectetions;
    }

    public struct HingeJointInformation
    {
        public HingeJointInformation(LimbBehaviour limbBehaviour)
        {
            if (limbBehaviour.Joint != null)
            {
                hasInformation = true;
                jointAngleLimits = limbBehaviour.Joint.limits;
                connectedAnchor = limbBehaviour.Joint.connectedAnchor;
                anchor = limbBehaviour.Joint.anchor;
                connectedBodyPath = UtilityCoreMethods.GetHierarchyPath(limbBehaviour.transform.root, limbBehaviour.Joint.connectedBody.transform);
                connectedBodyRotation = limbBehaviour.Joint.connectedBody.transform.rotation;
                attachedBodyPath = UtilityCoreMethods.GetHierarchyPath(limbBehaviour.transform.root, limbBehaviour.Joint.attachedRigidbody.transform);
                useLimits = limbBehaviour.Joint.useLimits;
                breakForce = limbBehaviour.Joint.breakForce;
                breakTorque = limbBehaviour.Joint.breakTorque;
            }
            else
            {
                hasInformation = false;
                jointAngleLimits = new JointAngleLimits2D();
                connectedAnchor = new Vector2();
                anchor = new Vector2();
                connectedBodyPath = "";
                attachedBodyPath = "";
                connectedBodyRotation = new Quaternion();
                useLimits = false;
                breakForce = 0;
                breakTorque = 0;
            }
            LimbBehaviour[] findedAttachedHingePath = limbBehaviour.ConnectedLimbs.Where(connectedLimb =>
            {
                if (connectedLimb.Joint != null)
                {
                    if (connectedLimb.Joint.connectedBody.transform == limbBehaviour.transform)
                    {
                        return true;
                    }
                }
                return false;
            }).ToArray();
            attachedHingePaths = new string[findedAttachedHingePath.Length];
            for (int i = 0; i < findedAttachedHingePath.Length; i++)
            {
                attachedHingePaths[i] = UtilityCoreMethods.GetHierarchyPath(findedAttachedHingePath[i].transform.root, findedAttachedHingePath[i].transform);
            }
        }

        public bool hasInformation;
        public JointAngleLimits2D jointAngleLimits;
        public Vector2 connectedAnchor;
        public Vector2 anchor;
        public string connectedBodyPath;
        public string attachedBodyPath;
        public Quaternion connectedBodyRotation;
        public bool useLimits;
        public float breakForce;
        public float breakTorque;

        public string[] attachedHingePaths;
    }

    public static class UtilityCoreMethods
    {
        internal static void NoChildCollide(this GameObject instance)
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

        internal class DelayerCaller : MonoBehaviour
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

        public static void SetRefField<T>(this T obj, string nameField, object value)
        {
            typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).Where(field => field.Name == nameField).FirstOrDefault().SetValue(obj, value);
        }

        public static A GetRefField<T, A>(this T obj, string nameField)
        {
            return (A)typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).Where(field => field.Name == nameField).FirstOrDefault().GetValue(obj);
        }

        public static void DelayedInvoke(float delay, Action action)
        {
            new GameObject("HT_DELAY").AddComponent<DelayerCaller>().Init(action, delay);
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
    }

    public class LimbRecorder : MonoBehaviour
    {
        public List<LimbInformation> RecordedLimbInformations { get; private set; } = new List<LimbInformation>();

        private void Awake()
        {
            RecordLimbInformations();
        }

        public void RecordLimbInformations()
        {
            var personBehaviour = GetComponent<PersonBehaviour>();
            if (personBehaviour != null)
            {
                foreach (var limb in personBehaviour.Limbs)
                {
                    if (!limb.gameObject.activeSelf)
                    {
                        RecordedLimbInformations.Add(new LimbInformation(limb));
                    }
                }
            }
        }

        public LimbInformation[] GetRecordedLimbInformations()
        {
            return RecordedLimbInformations.ToArray();
        }
    }

    public struct SerializedObjects
    {
        public List<ObjectState> Objects;
    }

    public class RegrowthModule : MonoBehaviour
    {
        public PersonBehaviour PersonBehaviour;
        public LimbInformations LimbInformationsSerializeble;

        [SkipSerialisation]
        public LimbInformation[] LimbInformations
        {
            get
            {
                return LimbInformationsSerializeble.list;
            }
            set
            {
                LimbInformationsSerializeble.list = value;
            }
        }

        public LimbBehaviour[] BiggestPart
        {
            get
            {
                return GetBiggestPart(PersonBehaviour);
            }
        }

        public LimbBehaviour[] GetPart(LimbBehaviour limbBehaviour)
        {
            HashSet<LimbBehaviour> part = new HashSet<LimbBehaviour>();
            Queue<LimbBehaviour> queue = new Queue<LimbBehaviour>();
            queue.Enqueue(limbBehaviour);
            while (queue.Count > 0)
            {
                LimbBehaviour currentLimb = queue.Dequeue();
                if (!part.Add(currentLimb))
                {
                    continue;
                }
                foreach (LimbBehaviour cl in currentLimb.ConnectedLimbs)
                {
                    if (cl.NodeBehaviour.IsConnectedTo(currentLimb.NodeBehaviour) && !part.Contains(cl))
                    {
                        queue.Enqueue(cl);
                    }
                }
            }
            return part.Where(l => l.gameObject.activeSelf).ToArray();
        }

        [SkipSerialisation]
        public LimbBehaviour[] DestroyedLimbs
        {
            get
            {
                HashSet<LimbBehaviour> limbBehaviours;
                var rootLimb = LimbRoot;
                limbBehaviours = new HashSet<LimbBehaviour>(BiggestPart);
                return PersonBehaviour.Limbs.Where(l => !limbBehaviours.Contains(l)).ToArray();
            }
        }

        public LimbBehaviour NextDestroyedLimb
        {
            get
            {
                LimbBehaviour[] destroyedLimbs = DestroyedLimbs;
                HashSet<LimbBehaviour> limbBehaviours;
                var rootLimb = LimbRoot;

                limbBehaviours = new HashSet<LimbBehaviour>(BiggestPart);

                foreach (LimbBehaviour dl in destroyedLimbs)
                {
                    HingeJointInformation info = GetLimbInformation(dl).hingeJointInformation;
                    if (info.hasInformation)
                    {
                        LimbBehaviour limba = GetLimbFromPath(info.attachedBodyPath);
                        if (limbBehaviours.Contains(limba))
                        {
                            return dl;
                        }
                    }

                    LimbBehaviour[] cls = GetAllConnectedLimbs(dl);
                    foreach (LimbBehaviour cl in cls)
                    {
                        if (limbBehaviours.Contains(cl))
                        {
                            return dl;
                        }
                    }
                }
                return null;
            }
        }

        public static LimbBehaviour[] GetBiggestPart(PersonBehaviour personBehaviour)
        {
            List<HashSet<LimbBehaviour>> parts = new List<HashSet<LimbBehaviour>>();
            HashSet<LimbBehaviour> visited = new HashSet<LimbBehaviour>();

            foreach (LimbBehaviour limb in personBehaviour.Limbs)
            {
                if (!visited.Contains(limb) && limb.gameObject.activeSelf)
                {
                    HashSet<LimbBehaviour> part = new HashSet<LimbBehaviour>();
                    Queue<LimbBehaviour> queue = new Queue<LimbBehaviour>();
                    queue.Enqueue(limb);

                    while (queue.Count > 0)
                    {
                        LimbBehaviour currentLimb = queue.Dequeue();
                        if (!part.Add(currentLimb))
                        {
                            continue;
                        }
                        visited.Add(currentLimb);

                        foreach (LimbBehaviour cl in currentLimb.ConnectedLimbs)
                        {
                            if (cl.NodeBehaviour.IsConnectedTo(currentLimb.NodeBehaviour) && cl.gameObject.activeSelf && !part.Contains(cl))
                            {
                                queue.Enqueue(cl);
                            }
                        }
                    }
                    parts.Add(part);
                }
            }
            HashSet<LimbBehaviour> biggestPart = parts.OrderByDescending(p => p.Count).FirstOrDefault();
            return biggestPart?.ToArray() ?? new LimbBehaviour[0];
        }

        public List<RegrowthModuleCallBack> callBacks = new List<RegrowthModuleCallBack>();
        public bool Debugging = true;
        public Controller controller;
        public bool Active = true;
        public Action onCollectedInfo;

        [SkipSerialisation]
        public LimbBehaviour LimbRoot => PersonBehaviour.Limbs.Where(l => l.NodeBehaviour.IsRoot).First();

        private void Start()
        {
            PersonBehaviour = gameObject.GetComponent<PersonBehaviour>();

            // Use cached information if exists
            if (RegrowthModuleCache.IsCached(PersonBehaviour))
            {
                LimbInformations = RegrowthModuleCache.GetLimbInformations(PersonBehaviour);
            }
            else
            {
                CollectInformationNew();
            }
        }

        private void InitializeWithRecordedInformation(LimbInformation[] recordedLimbInformations)
        {
            if (recordedLimbInformations != null && recordedLimbInformations.Length > 0)
            {
                LimbInformations = recordedLimbInformations;
                if (onCollectedInfo != null)
                {
                    onCollectedInfo.Invoke();
                }
            }
        }

        public LimbBehaviour[] GetAllConnectedLimbs(LimbBehaviour limb)
        {
            List<LimbBehaviour> connectedLimbs = new List<LimbBehaviour>();
            LimbInformation oldInformation = GetLimbInformation(limb);
            foreach (string connectedLimbPath in oldInformation.connectedLimbsPath)
            {
                connectedLimbs.Add(GetLimbFromPath(connectedLimbPath));
            }
            return connectedLimbs.ToArray();
        }

        public LimbBehaviour[] GetActualConnectedLimbs(LimbBehaviour limb)
        {
            List<LimbBehaviour> connectedLimbs = new List<LimbBehaviour>();
            LimbInformation actualInformation = new LimbInformation(limb);
            foreach (string connectedLimbPath in actualInformation.connectedLimbsPath)
            {
                connectedLimbs.Add(GetLimbFromPath(connectedLimbPath));
            }
            return connectedLimbs.ToArray();
        }

        private void CollectInformationNew() => StartCoroutine(CollectInformationNewCor());

        private IEnumerator CollectInformationNewCor()
        {
            yield return new WaitForSecondsRealtime(0.015f);
            if (LimbInformationsSerializeble.list == null || LimbInformationsSerializeble.list.Length == 0)
            {
                if (RegrowthModuleCache.IsCached(PersonBehaviour))
                {
                    LimbInformations = RegrowthModuleCache.GetLimbInformations(PersonBehaviour);
                    if (onCollectedInfo != null)
                    {
                        onCollectedInfo.Invoke();
                    }
                }
                else
                {
                    CollectInformation();
                }
            }
            else
            {
            }
        }

        private void InitializeCallBack()
        {
            foreach (LimbBehaviour limb in PersonBehaviour.Limbs)
            {
                RegrowthModuleCallBack callBack = limb.gameObject.GetOrAddComponent<RegrowthModuleCallBack>();
                callBack.Start();
                callBack.RegrowthModule = this;
                callBacks.Add(callBack);
            }
        }

        private void CollectInformation() => StartCoroutine(CollectInformationCoroutine());

        #region RegrowthLogic

        private void ReabilityLimb(LimbBehaviour limbBehaviour)
        {
            try
            {
                foreach (PhysicalBehaviour.Penetration penetration in limbBehaviour.PhysicalBehaviour.penetrations)
                {
                    typeof(PhysicalBehaviour).GetMethod("DestroyStabJoint", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(limbBehaviour.PhysicalBehaviour, new object[] { penetration });
                }
                foreach (PhysicalBehaviour.Penetration victimPenetration in limbBehaviour.PhysicalBehaviour.victimPenetrations)
                {
                    victimPenetration.Active = false;
                    limbBehaviour.PhysicalBehaviour.stabWoundCount--;
                    limbBehaviour.PhysicalBehaviour.beingStabbedBy.Remove(limbBehaviour.PhysicalBehaviour);
                    victimPenetration.Stabber.penetrations.Remove(victimPenetration);
                    typeof(PhysicalBehaviour).GetMethod("UndoPenetrationNoCollision", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(limbBehaviour.PhysicalBehaviour, new object[] { victimPenetration });
                }
                limbBehaviour.PhysicalBehaviour.victimPenetrations.Clear();
            }
            catch (Exception Exeption)
            {
            }

            Collider2D[] colliders = limbBehaviour.gameObject.GetComponentsInChildren<Collider2D>();
            Renderer[] renderers = limbBehaviour.gameObject.GetComponentsInChildren<Renderer>();
            Rigidbody2D[] rbs = limbBehaviour.gameObject.GetComponentsInChildren<Rigidbody2D>();

            foreach (Collider2D coll in colliders)
            {
                coll.enabled = true;
            }
            foreach (Renderer rend in renderers)
            {
                rend.enabled = true;
            }
            foreach (Rigidbody2D rb in rbs)
            {
                rb.simulated = true;
            }

            limbBehaviour.gameObject.SetActive(true);
            limbBehaviour.PhysicalBehaviour.isDisintegrated = false;
            if (limbBehaviour.gameObject.TryGetComponent(out FreezeBehaviour freezeBehaviour))
            {
                Destroy(freezeBehaviour);
            }
            limbBehaviour.PhysicalBehaviour.rigidbody.bodyType = RigidbodyType2D.Dynamic;
            limbBehaviour.SkinMaterialHandler.damagePoints = new Vector4[limbBehaviour.SkinMaterialHandler.damagePoints.Length];
            limbBehaviour.SkinMaterialHandler.damagePointTimeStamps = new float[limbBehaviour.SkinMaterialHandler.damagePointTimeStamps.Length];
            limbBehaviour.SkinMaterialHandler.currentDamagePointCount = 0;
            limbBehaviour.SkinMaterialHandler.Sync();
            ClearOnCollisionBuffer(limbBehaviour.PhysicalBehaviour);
            if (controller != null)
            {
                if (controller.DestroyWires)
                {
                    DestroyWires(limbBehaviour);
                }
                else
                {
                    BackWires(limbBehaviour);
                }
            }
            else
            {
                DestroyWires(limbBehaviour);
            }
        }

        public static void ClearOnCollisionBuffer(PhysicalBehaviour physicalBehaviour)
        {
            Type type = Type.GetType("PhysicalBehaviour+ColliderBoolPair, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            ConstructorInfo constructor = type.GetConstructor(new Type[] { typeof(bool), typeof(Collider2D) });
            object[] array = physicalBehaviour.GetRefField<PhysicalBehaviour, object[]>("onCollisionStayBuffer");
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = null;
            }
            physicalBehaviour.SetRefField("onCollisionStayBuffer", array);
        }

        public void ReabilityJoint(LimbBehaviour limbBehaviour)
        {
            LimbInformation limbInfo = GetLimbInformation(limbBehaviour);
            HingeJointInformation jointInfo = limbInfo.hingeJointInformation;
            LimbBehaviour probablyGoodLimb = null;

            try
            {
                probablyGoodLimb = GetLimbFromPath(jointInfo.attachedHingePaths.Where(path => GetLimbFromPath(path).gameObject.activeSelf)?.First());
            }
            catch { }
            if (probablyGoodLimb != null)
            {
                limbBehaviour.transform.position = probablyGoodLimb.transform.position;
                limbBehaviour.PhysicalBehaviour.rigidbody.rotation = probablyGoodLimb.PhysicalBehaviour.rigidbody.rotation; // используем Rigidbody2D.rotation, потому что HingeJoint2D при установке upperBody не использует позицию Transform.rotation, он использует Rigidbody2D.position для расчёта угла между объектом и connectedBody. Если мы используем transform.rotation, то будет отклонение в несколько градусов. Это ещё один из длинных комментариев.
                if (GetEmptyJoint(probablyGoodLimb) != null)
                {
                    HingeJoint2D probablyGoodLimbJoint = GetEmptyJoint(probablyGoodLimb);
                    LimbInformation pInfo = GetLimbInformation(probablyGoodLimb);
                    HingeJointInformation probablyGoodLimbJointInfo = pInfo.hingeJointInformation;
                    probablyGoodLimbJoint.autoConfigureConnectedAnchor = false;
                    probablyGoodLimbJoint.anchor = probablyGoodLimbJointInfo.anchor;
                    probablyGoodLimbJoint.connectedAnchor = probablyGoodLimbJointInfo.connectedAnchor;
                    probablyGoodLimb.BreakingThreshold = pInfo.breakingThresold;
                    probablyGoodLimbJoint.breakForce = probablyGoodLimbJointInfo.breakForce;
                    probablyGoodLimbJoint.breakTorque = probablyGoodLimbJointInfo.breakTorque;
                    probablyGoodLimbJoint.limits = probablyGoodLimbJointInfo.jointAngleLimits;
                    probablyGoodLimbJoint.connectedBody = limbBehaviour.PhysicalBehaviour.rigidbody;
                    probablyGoodLimbJoint.useLimits = probablyGoodLimbJointInfo.useLimits;
                    probablyGoodLimbJoint.useMotor = true;
                    probablyGoodLimb.Joint = probablyGoodLimbJoint;
                    probablyGoodLimb.HasJoint = true;
                    probablyGoodLimb.IsDismembered = false;
                    probablyGoodLimb.SendMessage("SetupJoint");
                    if (probablyGoodLimb.gameObject.TryGetComponent(out GoreStringBehaviour goreStringBehaviour))
                    {
                        goreStringBehaviour.DestroyJoint();
                    }
                    UtilityCoreMethods.DelayedInvoke(2f, () => probablyGoodLimb.Joint.autoConfigureConnectedAnchor = true);
                }
            }
            else if (jointInfo.hasInformation)
            {
                LimbBehaviour connectedBody = GetLimbFromPath(jointInfo.connectedBodyPath);
                if (connectedBody.gameObject.activeSelf)
                {
                    limbBehaviour.transform.position = connectedBody.transform.position;
                    limbBehaviour.PhysicalBehaviour.rigidbody.rotation = connectedBody.PhysicalBehaviour.rigidbody.rotation;  // используем Rigidbody2D.rotation, потому что HingeJoint2D при установке upperBody не использует позицию Transform.rotation, он использует Rigidbody2D.position для расчёта угла между объектом и connectedBody. Если мы используем transform.rotation, то будет отклонение в несколько градусов. Это ещё один из длинных комментариев.
                    HingeJoint2D joint = GetEmptyJoint(limbBehaviour);

                    joint.autoConfigureConnectedAnchor = false;
                    joint.anchor = jointInfo.anchor;
                    joint.connectedAnchor = jointInfo.connectedAnchor;
                    limbBehaviour.BreakingThreshold = limbInfo.breakingThresold;
                    joint.breakForce = jointInfo.breakForce;
                    joint.breakTorque = jointInfo.breakTorque;
                    joint.limits = jointInfo.jointAngleLimits;
                    joint.connectedBody = connectedBody.PhysicalBehaviour.rigidbody;
                    joint.useLimits = jointInfo.useLimits;
                    joint.useMotor = true;
                    limbBehaviour.Joint = joint;
                    limbBehaviour.HasJoint = true;
                    limbBehaviour.IsDismembered = false;
                    limbBehaviour.SendMessage("SetupJoint");
                    UtilityCoreMethods.DelayedInvoke(2f, () => limbBehaviour.Joint.autoConfigureConnectedAnchor = true);
                }
            }
            PersonBehaviour.gameObject.NoChildCollide(); // вырубаем коллизии между джоинтами
        }

        public void BackWires(LimbBehaviour limbBehaviour)
        {
            foreach (LineRenderer wire in limbBehaviour.gameObject.GetComponentsInChildren<LineRenderer>())
            {
                if (wire.gameObject.name == "Wire")
                {
                    wire.enabled = true;
                }
            }
        }

        public void DestroyWires(LimbBehaviour limbBehaviour)
        {
            foreach (Component component in limbBehaviour.gameObject.GetComponents<Component>())
            {
                try
                {
                    Hover hover = (Hover)component;
                    if (hover != null)
                    {
                        Destroy(hover);
                    }
                }
                catch { }
            }
        }

        public void ConnectToLimbSystem(LimbBehaviour limbBehaviour)
        {
            LimbInformation limbInfo = GetLimbInformation(limbBehaviour);
            limbBehaviour.ConnectedLimbs = new List<LimbBehaviour>();
            // LimbBehaviour.ConnectedLimbs
            foreach (string connectedLimbPath in limbInfo.connectedLimbsPath) // убедитесь что лимбы не уничтожены
            {
                LimbBehaviour connectedLimb = GetLimbFromPath(connectedLimbPath);
                if (DestroyedLimbs.Contains(connectedLimb))
                {
                    continue;
                }
                connectedLimb.IsDismembered = false;
                limbBehaviour.ConnectedLimbs.Add(connectedLimb);
                connectedLimb.ConnectedLimbs.Add(limbBehaviour);
            }
            // limbBehaviour.SkinMaterialHandler.adjacentLimbs
            limbBehaviour.SkinMaterialHandler.adjacentLimbs = new SkinMaterialHandler[limbInfo.adjacentLimbs.Length];
            for (int i = 0; i < limbInfo.adjacentLimbs.Length; i++)
            {
                limbBehaviour.SkinMaterialHandler.adjacentLimbs[i] = GetLimbFromPath(limbInfo.adjacentLimbs[i]).SkinMaterialHandler;
            }
            // limbBehaviour.CirculationBehaviour.PushesTo
            limbBehaviour.CirculationBehaviour.PushesTo = new CirculationBehaviour[limbInfo.pushToLimbsPath.Length];
            for (int i = 0; i < limbInfo.pushToLimbsPath.Length; i++)
            {
                limbBehaviour.CirculationBehaviour.PushesTo[i] = GetLimbFromPath(limbInfo.pushToLimbsPath[i]).CirculationBehaviour;
                if (limbBehaviour.CirculationBehaviour.PushesTo[i].gameObject.activeSelf)
                {
                    limbBehaviour.CirculationBehaviour.PushesTo[i].IsDisconnected = false;
                }
            }
            if (limbInfo.sourcePath != "")
            {
                limbBehaviour.CirculationBehaviour.Source = GetLimbFromPath(limbInfo.sourcePath).CirculationBehaviour;
            }
            limbBehaviour.CirculationBehaviour.IsPump = limbBehaviour.CirculationBehaviour.WasInitiallyPumping;
            limbBehaviour.CirculationBehaviour.IsDisconnected = false;
            // NodeBehaviour
            NodeInformation nodeInformation = limbInfo.nodeInformation;
            limbBehaviour.NodeBehaviour.Connections = new ConnectedNodeBehaviour[nodeInformation.connectionsTransformPaths.Length];
            for (int i = 0; i < nodeInformation.connectionsTransformPaths.Length; i++)
            {
                limbBehaviour.NodeBehaviour.Connections[i] = GetLimbFromPath(nodeInformation.connectionsTransformPaths[i]).NodeBehaviour;
            }
            foreach (KeyValuePair<string, int> connectedNodePath in nodeInformation.indexInConnectetions)
            {
                LimbBehaviour connectedNode = GetLimbFromPath(connectedNodePath.Key);
                connectedNode.NodeBehaviour.Connections[connectedNodePath.Value] = limbBehaviour.NodeBehaviour;
            }
            limbBehaviour.NodeBehaviour.Value = nodeInformation.value;
            limbBehaviour.NodeBehaviour.IsRoot = nodeInformation.isRoot;
            limbBehaviour.NodeBehaviour.RootPropagation();
            limbBehaviour.IsDismembered = false;
        }

        public void RegrowthNearestLimb()
        {
            LimbBehaviour nextDestroyedLimb = NextDestroyedLimb;
            if (nextDestroyedLimb != null)
            {
                RegrowthLimb(nextDestroyedLimb, null, false, 1, false);
            }
        }

        public void RegrowthAll()
        {
            StartCoroutine(RegrowthAllCoroutine());
        }

        // limbBehaviour - кончность которую надо отрастить
        // requester - конечность которая просит другую конечность отрастится чтобы отращивание limbBehaviour был возможен, короче пропагация, если вы вызываете этот метод - оставляете этот параметр null
        // onlyNeeded - если true то отрастит только нужные для отращивания limbBehaviour, к примеру вы отрываете полностью руку, сначало lowerArm попросит upperArm отрастится и на этом всё закончится
        // а если к примеру от upperBody был оторван middleBody то пропагация не пойдёт к нему, но если onlyNeeded на false, но пропагация проверит соседние конечности если они дальше по distanceBrain
        // limitRegrowthLimbs - ограничение по отращиванию конечностей
        // denyRoot - если вам каким то хуем надо голову отрастить - ставьте на false
        public void RegrowthLimb(LimbBehaviour limbBehaviour, LimbBehaviour requester = null, bool onlyNeeded = true, int limitRegrowthLimbs = int.MaxValue, bool denyRoot = false)
        {
            if (!Active)
            {
                return;
            }
            if (!PersonBehaviour.IsAlive())
            {
                if (controller != null)
                {
                    if (controller.DontRegrowthDead)
                    {
                        return;
                    }
                }
            }
            if (!LimbRoot.gameObject.activeSelf && denyRoot)
            {
                if (controller != null)
                {
                    if (controller.DontRegrowthWithoutRoot)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            if (limbBehaviour.NodeBehaviour.IsRoot && denyRoot)
            {
                return;
            }
            if (limitRegrowthLimbs <= 0)
            {
                return;
            }

            bool fakeLimbsCreated = false;
            PersonBehaviour fakePerson = null;
            if (requester == null)
            {
                if (controller != null)
                {
                    if (controller.createFakeLimbs)
                    {
                        if (PersonBehaviour.Limbs.Where(l => l.gameObject.activeSelf && !l.NodeBehaviour.IsConnectedToRoot).ToArray().Length > 0) // проверяем нужно ли копировать
                        {
                            fakeLimbsCreated = true;
                            SerializedObjects fakePersonSerialized = new SerializedObjects();
                            fakePersonSerialized.Objects = ObjectStateConverter.Convert(PersonBehaviour.Limbs.Select(l => l.gameObject).ToList(), PersonBehaviour.gameObject.transform.position).ToList();
                            GameObject[] fakePersonObjects = ObjectStateConverter.Convert(fakePersonSerialized.Objects, PersonBehaviour.gameObject.transform.position);
                            fakePerson = fakePersonObjects[0].gameObject.GetComponent<PersonBehaviour>();
                            foreach (LimbBehaviour fakeLimb in fakePerson.Limbs)
                            {
                                fakeLimb.PhysicalBehaviour.SpawnSpawnParticles = false;
                            }
                            fakePerson.Limbs.Where(l => l.NodeBehaviour.IsRoot).First().PhysicalBehaviour.Disintegrate();
                            if (fakePerson.gameObject.TryGetComponent(out RegrowthModule.Controller controller))
                            {
                                Destroy(controller);
                            }
                            if (fakePerson.gameObject.TryGetComponent<RegrowthModule>(out RegrowthModule regrowthModule))
                            {
                                Destroy(regrowthModule);
                            }
                            Collider2D[] fakePersonColliders = fakePerson.GetComponentsInChildren<Collider2D>();
                            Collider2D[] personColliders = PersonBehaviour.gameObject.GetComponentsInChildren<Collider2D>();
                            foreach (Collider2D fakePersonCollider in fakePersonColliders)
                            {
                                foreach (Collider2D personCollider in personColliders)
                                {
                                    Physics2D.IgnoreCollision(fakePersonCollider, personCollider, true);
                                }
                            }
                        }
                    }
                }

                foreach (LimbBehaviour connectedLimb in limbBehaviour.ConnectedLimbs.Where(l => l.DistanceToBrain > limbBehaviour.DistanceToBrain && l.NodeBehaviour.IsConnectedToRoot))
                {
                    connectedLimb.IsDismembered = true;
                }
                foreach (LimbBehaviour limbToCrush in DestroyedLimbs)
                {
                    DisintegrationCounterBehaviour disCounter = gameObject.GetComponent<DisintegrationCounterBehaviour>();
                    if (controller != null)
                    {
                        if (controller.DisintegrationCounterSelfControl)
                        {
                            disCounter.DisintegrationCount = DestroyedLimbs.Length - 1;
                        }
                    }
                    else
                    {
                        disCounter.DisintegrationCount = DestroyedLimbs.Length - 1;
                    }
                    if (controller != null)
                    {
                        switch (controller.destroyType)
                        {
                            case Controller.LimbDestroyType.Custom:
                                if (controller.DestroyLimbAction != null)
                                {
                                    controller.DestroyLimbAction.Invoke(limbToCrush);
                                }
                                break;

                            case Controller.LimbDestroyType.Crush:
                                limbToCrush.Crush();
                                break;

                            case Controller.LimbDestroyType.Disintegrate:
                                ModAPI.CreateParticleEffect("Disintegration", limbToCrush.transform.position);
                                limbToCrush.PhysicalBehaviour.Disintegrate();
                                break;

                            default:
                                limbToCrush.Crush();
                                break;
                        }
                    }
                    else
                    {
                        limbToCrush.Crush();
                    }
                }
            }
            List<LimbBehaviour> connectedLimbsNeedRegrowth = new List<LimbBehaviour>(); // конечности которые требуют отращивания перед тем как отрастить конечность которую вы передали в параметр
            LimbInformation limbInfo = GetLimbInformation(limbBehaviour);
            foreach (string connectedLimbPath in limbInfo.connectedLimbsPath)
            {
                LimbBehaviour connectedLimb = GetLimbFromPath(connectedLimbPath);
                if (DestroyedLimbs.Contains(connectedLimb) && connectedLimb != requester)
                {
                    if (onlyNeeded)
                    {
                        connectedLimbsNeedRegrowth.Add(connectedLimb);
                    }
                    else
                    {
                        connectedLimbsNeedRegrowth.Add(connectedLimb);
                    }
                }
            }
            LimbBehaviour[] oldDl = DestroyedLimbs;
            ReabilityLimb(limbBehaviour);
            ConnectToLimbSystem(limbBehaviour);
            if (fakeLimbsCreated)
            {
                foreach (LimbBehaviour fakeLimb in fakePerson.Limbs)
                {
                    IEnumerable<LimbBehaviour> originalLimb = PersonBehaviour.Limbs.Where(l => l.name == fakeLimb.name);
                    if (originalLimb.ToArray().Length > 0)
                    {
                        if (originalLimb.First().NodeBehaviour.IsConnectedToRoot)
                        {
                            fakeLimb.PhysicalBehaviour.Disintegrate();
                        }
                    }
                }
                controller.AfterCreateFakeLimbs(fakePerson);
            }
            foreach (LimbBehaviour connectedLimbToRegrowth in connectedLimbsNeedRegrowth)
            {
                RegrowthLimb(connectedLimbToRegrowth, limbBehaviour, onlyNeeded, limitRegrowthLimbs - 1);
            }
            ReabilityJoint(limbBehaviour);
            if (limbBehaviour.gameObject.TryGetComponent(out GoreStringBehaviour goreStringBehaviour))
            {
                goreStringBehaviour.DestroyJoint();
            }
            if (limbBehaviour.gameObject.TryGetComponent(out GripBehaviour gripBehaviour))
            {
                gripBehaviour.DropObject();
            }
            if (limbBehaviour.transform.root.gameObject.TryGetComponent<AudioSourceTimeScaleBehaviour>(out AudioSourceTimeScaleBehaviour audioSourceTimeScaleBehaviour))
            {
                MethodInfo startAudioSource = typeof(AudioSourceTimeScaleBehaviour).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
                startAudioSource.Invoke(audioSourceTimeScaleBehaviour, new object[] { });
            }
            if (controller != null)
            {
                controller.OnRegrowthLimb(limbBehaviour);
            }
        }

        #endregion RegrowthLogic

        #region Coroutines

        private IEnumerator CollectInformationCoroutine()
        {
            yield return new WaitForEndOfFrame();
            LimbInformations = new LimbInformation[PersonBehaviour.Limbs.Length];
            for (int i = 0; i < PersonBehaviour.Limbs.Length; i++)
            {
                LimbInformations[i] = new LimbInformation(PersonBehaviour.Limbs[i]);
            }
            yield return new WaitForEndOfFrame();
            if (onCollectedInfo != null)
            {
                onCollectedInfo.Invoke();
            }
        }

        private IEnumerator RepositingLimbsCoroutine()
        {
            for (int i = 0; i < PersonBehaviour.Limbs.Length; i++)
            {
                yield return new WaitForEndOfFrame();
                foreach (LimbInformation limbInfo in LimbInformations.OrderBy(a => a.distanceBrain))
                {
                    LimbBehaviour limb = GetLimbFromPath(limbInfo.transformPath);
                    HingeJointInformation jointInfo = limbInfo.hingeJointInformation;
                    if (jointInfo.hasInformation && limb.HasJoint && limb.Joint?.connectedBody != null)
                    {
                        LimbBehaviour connectedBody = GetLimbFromPath(jointInfo.connectedBodyPath);
                        Vector2 Displacement = connectedBody.transform.TransformPoint(jointInfo.connectedAnchor) - connectedBody.gameObject.transform.TransformPoint(limb.Joint.connectedAnchor);
                        limb.transform.position = (Vector2)limb.transform.position + Displacement;
                    }
                }
            }
        }

        private HingeJoint2D GetEmptyJoint(LimbBehaviour limbBehaviour)
        {
            if (limbBehaviour.Joint != null)
            {
                return limbBehaviour.Joint;
            }
            else
            {
                LimbInformation limbInfo = GetLimbInformation(limbBehaviour);
                HingeJointInformation jointInfo = limbInfo.hingeJointInformation;
                if (jointInfo.hasInformation)
                {
                    limbBehaviour.Joint = limbBehaviour.gameObject.AddComponent<HingeJoint2D>();
                    limbBehaviour.HasJoint = true;
                    return limbBehaviour.Joint;
                }
                else
                {
                    return null;
                }
            }
        }

        private IEnumerator RegrowthAllCoroutine()
        {
            int countDestroyedLimbs = DestroyedLimbs.Length;
            for (int i = 0; i < countDestroyedLimbs; i++)
            {
                yield return new WaitForEndOfFrame();
                RegrowthNearestLimb();
            }
        }

        private IEnumerator RepositingLimbCoroutine(LimbBehaviour limbBehaviour)
        {
            yield return new WaitForEndOfFrame();
            LimbInformation limbInfo = GetLimbInformation(limbBehaviour);
            HingeJointInformation jointInfo = limbInfo.hingeJointInformation;
            if (jointInfo.hasInformation && limbBehaviour.HasJoint && !limbInfo.nodeInformation.isRoot)
            {
                LimbBehaviour connectedBody = GetLimbFromPath(jointInfo.connectedBodyPath);
                Vector2 Displacement = connectedBody.transform.TransformPoint(jointInfo.connectedAnchor) - connectedBody.gameObject.transform.TransformPoint(limbBehaviour.Joint.connectedAnchor);
                limbBehaviour.transform.position = (Vector2)limbBehaviour.transform.position + Displacement;
            }
        }

        #endregion Coroutines

        #region CallBack

        public Action<LimbBehaviour> OnLimbDestroyed;

        public class RegrowthModuleCallBack : MonoBehaviour
        {
            public RegrowthModule RegrowthModule;
            public LimbBehaviour LimbBehaviour;
            public DisintigratesInfo disintigratesInfo;

            [SkipSerialisation]
            public bool started = false;

            public void Start()
            {
                if (started) return;
                started = true;
                LimbBehaviour = gameObject.GetComponent<LimbBehaviour>();
                disintigratesInfo = gameObject.GetOrAddComponent<DisintigratesInfo>();
                LimbBehaviour.PhysicalBehaviour.OnDisintegration += PhysicalBehaviour_OnDisintegration;
            }

            private void PhysicalBehaviour_OnDisintegration(object sender, EventArgs e)
            {
                //disintigratesInfo.Call();
            }
        }

        #endregion CallBack

        #region Other

        public LimbInformation GetLimbInformation(LimbBehaviour limbBehaviour)
        {
            return LimbInformations.Where(limbInfo => limbInfo.transformPath == UtilityCoreMethods.GetHierarchyPath(limbBehaviour.transform.root, limbBehaviour.transform)).First();
        }

        public LimbBehaviour GetLimbFromPath(string path)
        {
            return PersonBehaviour.Limbs.Where(limb => UtilityCoreMethods.GetHierarchyPath(limb.transform.root, limb.transform) == path).First();
        }

        public void RepositingLimbs()
        {
            StartCoroutine(RepositingLimbsCoroutine());
        }

        public void RepositingLimb(LimbBehaviour limbBehaviour)
        {
            StartCoroutine(RepositingLimbCoroutine(limbBehaviour));
        }

        public class DisintigratesInfo : MonoBehaviour
        {
            public LimbBehaviour limbBehaviour;
            public List<Collider2D> colliders;
            public List<Renderer> renderers;
            public List<Rigidbody2D> rbs;

            [SkipSerialisation]
            public bool collected = false;

            private void Start()
            {
                if (collected) return;

                limbBehaviour = gameObject.GetComponent<LimbBehaviour>();
                Call();
            }

            public void Call()
            {
                collected = true;
                colliders = limbBehaviour.gameObject.GetComponentsInChildren<Collider2D>().Where(c => c.enabled).ToList();
                renderers = limbBehaviour.gameObject.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToList();
                rbs = limbBehaviour.gameObject.GetComponentsInChildren<Rigidbody2D>().Where(r => r.simulated).ToList();
            }
        }

        #endregion Other

        #region Controllers

        [RequireComponent(typeof(RegrowthModule))]
        [DisallowMultipleComponent]
        public abstract class Controller : MonoBehaviour
        {
            public enum ModeRegrowth
            {
                Active,
                Passive
            }

            public enum LimbDestroyType
            {
                Custom,
                Crush,
                Disintegrate
            }

            public virtual bool Active => true;
            public virtual ModeRegrowth mode => ModeRegrowth.Active;
            public virtual float passiveTimeUpdate => 0.01f;
            public virtual bool createFakeLimbs => false;
            public virtual LimbDestroyType destroyType => LimbDestroyType.Crush;
            public RegrowthModule regrowthModule => m_regrowthModule;
            public virtual bool DontRegrowthWithoutRoot => true;
            public virtual bool DisintegrationCounterSelfControl => true;
            public virtual bool DontRegrowthDead => true;
            public virtual bool DestroyWires => true;
            public virtual Action<LimbBehaviour> DestroyLimbAction => null;
            protected RegrowthModule m_regrowthModule;

            protected void Awake()
            {
                m_regrowthModule = gameObject.GetComponent<RegrowthModule>();
                m_regrowthModule.controller = this;
            }

            protected void Start()
            {
                StartCoroutine(PassiveHandler());
                AfterStart();
            }

            public virtual void AfterStart()
            { }

            private IEnumerator PassiveHandler()
            {
                yield return new WaitForSeconds(passiveTimeUpdate);
                if (mode == ModeRegrowth.Passive)
                {
                    OnPassiveUpdate();
                }
                StartCoroutine(PassiveHandler());
            }

            public virtual void OnPassiveUpdate()
            {
            }

            public virtual void AfterCreateFakeLimbs(PersonBehaviour fakePerson)
            {
            }

            public virtual void OnRegrowthLimb(LimbBehaviour limbBehaviour)
            {
            }
        }

        #endregion Controllers
    }
}