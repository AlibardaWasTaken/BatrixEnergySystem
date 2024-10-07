using System;
using System.Collections.Generic;
using UnityEngine;

namespace ENSYS
{

    public class DurabilityTagged : MonoBehaviour
    {
        public bool shouldPatchFootBlood = true;
        public float FootBloodMult = 0.1f;
        public PersonBehaviour person;
        private void Awake()
        {
            if (this.TryGetComponent<PersonBehaviour>(out person) == false)
                Destroy(this);

            AfterAwake();
        }
        public virtual void AfterAwake()
        { }
        public virtual void AfterInit()
        { }

        public Dictionary<LimbBehaviour, DurabilityBaseLimb> appliedDurLmb = new Dictionary<LimbBehaviour, DurabilityBaseLimb>();
        public void Init(Type baseDurlimbType)
        {

            if (ENSYSCore.DurabilitySystemsUsers.ContainsKey(this.transform.root) == false)
                ENSYSCore.DurabilitySystemsUsers.Add(this.transform.root, this);

            appliedDurLmb.Clear();

            foreach (var lmb in person.Limbs)
            {
                DurabilityBaseLimb durlmb = null;
                if (lmb.gameObject.GetComponent(baseDurlimbType) as DurabilityBaseLimb == null)
                {
                    durlmb = lmb.gameObject.AddComponent(baseDurlimbType) as DurabilityBaseLimb;

                }
                else
                {
                    durlmb = lmb.gameObject.GetComponent(baseDurlimbType) as DurabilityBaseLimb;
                }
                durlmb.creator = this;
                appliedDurLmb.Add(lmb, durlmb);
                lmb.ImmuneToDamage = true;
                lmb.CirculationBehaviour.ImmuneToDamage = true;

            }
            AfterInit();
        }

        protected virtual void OnDestroy()
        {
            if (person == null)
                return;

            if (ENSYSCore.DurabilitySystemsUsers.ContainsKey(this.transform.root) == true)
                ENSYSCore.DurabilitySystemsUsers.Remove(this.transform.root);

            foreach (var dur in appliedDurLmb)
            {
                Destroy(dur.Value);
            }

            foreach (var lmb in person.Limbs)
            {
                lmb.ImmuneToDamage = false;
                lmb.CirculationBehaviour.ImmuneToDamage = false;
            }
            OnDestroyTrigger();
        }

        public virtual void OnDestroyTrigger()
        { }

    }
    public abstract class DurabilityBaseLimb : MonoBehaviour
    {
        public DurabilityTagged creator;


        public LimbBehaviour limb;
        public PersonBehaviour Person;
        public CirculationBehaviour CirculationBehaviour;
        public SkinMaterialHandler SkinMaterialHandler;


        private void Start()
        {
            limb = this.GetComponent<LimbBehaviour>();
            limb.ImmuneToDamage = true;
            Person = limb.Person;
            CirculationBehaviour = limb.CirculationBehaviour;
            AfterStart();
        }

        public virtual void Patch_Damage(float damage)
        {
            if (UserPreferenceManager.Current.StopAnimationOnDamage && !this.limb.IsZombie && damage > 15.5f && this.limb.NodeBehaviour.IsConnectedToRoot)
            {
                this.Person.OverridePoseIndex = -1;
            }
            this.limb.Health -= damage;
            if (this.limb.Health <= 0f)
            {
                this.CirculationBehaviour.IsPump = false;
            }
        }

        public virtual void AfterStart()
        { }

        public virtual void Stabbed(Stabbing stab)
        {

        }

        public virtual void Patch_Slice()
        {

        }


        public virtual void Shot(Shot shot)
        {

        }

        public virtual void Patch_Crush()
        {

        }


        public virtual void ExitShot(Shot shot)
        {

        }

        public virtual void Unstabbed(Stabbing stabbing)
        {

        }

        public virtual void Cut(Vector2 point, Vector2 direction)
        {

        }

        public virtual void WaterImpact(float magnitude)
        {

        }



        public virtual void ActOnImpact(float impulse, Vector3 globalPosition)
        {

        }

    }


}
