using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ENSYS
{
    public abstract class AbstractLevelSystem : MonoBehaviour
    {
        protected int level = 1;
        public int maxlevel = 10;
        public virtual int Level
        {
            get => level;
            set
            {
                level = value;
                OnLevelChangedEvent?.Invoke(experience);
            }
        }

        protected int experience = 0;
        public virtual int Experience
        {
            get => experience;
            set
            {
                experience = value;
                OnExperienceChangedEvent?.Invoke(experience);
                CheckLevelUp();
            }
        }

        protected int experienceToNextLevel = 100;
        public virtual int ExperienceToNextLevel
        {
            get => experienceToNextLevel;
            protected set
            {
                experienceToNextLevel = value;
                OnExperienceToNextLevelChangedEvent?.Invoke(experienceToNextLevel);
            }
        }


        protected virtual void Awake()
        {

        }

        public UnityEvent<int> OnLevelUpEvent { get; } = new UnityEvent<int>();
        public UnityEvent<int> OnExperienceChangedEvent { get; } = new UnityEvent<int>();

        public UnityEvent<int> OnLevelChangedEvent { get; } = new UnityEvent<int>();
        public UnityEvent<int> OnExperienceToNextLevelChangedEvent { get; } = new UnityEvent<int>();

        public virtual void AddExperience(int amount)
        {
            Experience += amount;
        }

        protected virtual void CheckLevelUp()
        {
            if (level >= maxlevel)
                return;

            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                LevelUp();
            }
        }

        protected virtual void LevelUp()
        {
            Level++;
            OnLevelUpEvent?.Invoke(Level);
            ExperienceToNextLevel = CalculateExperienceToNextLevel();
        }

        protected abstract int CalculateExperienceToNextLevel();


    }





    public class LevelSystem : AbstractLevelSystem
    {
        public ILevelingStrategy LevelingStrategy { get; set; }
        private IExperienceGainer experienceGainer;

        protected override void Awake()
        {
            base.Awake();

            // Assign default leveling strategy if none is set
            if (LevelingStrategy == null)
            {
                LevelingStrategy = new LinearLevelingStrategy();
            }
            ExperienceToNextLevel = CalculateExperienceToNextLevel();

            // Initialize the experience gainer
            experienceGainer = GetComponent<IExperienceGainer>();
            if (experienceGainer != null)
            {
                experienceGainer.Initialize(this);
            }
            else
            {
                Debug.Log("No IExperienceGainer implementation found on this character.");
            }
        }

        private void Update()
        {
            experienceGainer?.OnUpdate();
        }

        protected override int CalculateExperienceToNextLevel()
        {
            return LevelingStrategy.GetExperienceToNextLevel(Level);
        }

    }




    public class LinearLevelingStrategy : ILevelingStrategy
    {
        public int BaseExperience { get; set; } = 100;
        public int ExperienceIncrement { get; set; } = 50;

        public int GetExperienceToNextLevel(int currentLevel)
        {
            return BaseExperience + ExperienceIncrement * (currentLevel - 1);
        }
    }

    public class ExponentialLevelingStrategy : ILevelingStrategy
    {
        public int BaseExperience { get; set; } = 100;
        public float Exponent { get; set; } = 1.5f;

        public int GetExperienceToNextLevel(int currentLevel)
        {
            return (int)(BaseExperience * Mathf.Pow(currentLevel, Exponent));
        }
    }






    public class CompositeExperienceGainer : MonoBehaviour, IExperienceGainer
    {
        private LevelSystem levelSystem;
        private List<IExperienceGainer> experienceGainers = new List<IExperienceGainer>();

        public void Initialize(LevelSystem levelSystem)
        {
            this.levelSystem = levelSystem;

            // Get all attached IExperienceGainer components
            GetComponents<IExperienceGainer>(experienceGainers);
            experienceGainers.Remove(this); // Remove self to avoid recursion

            // Initialize each experience gainer
            foreach (var gainer in experienceGainers)
            {
                gainer.Initialize(levelSystem);
            }
        }

        public void OnUpdate()
        {
            foreach (var gainer in experienceGainers)
            {
                gainer.OnUpdate();
            }
        }
    }


    //examples of gainers




    public class KillExperienceGainer : MonoBehaviour, IExperienceGainer
    {
        private LevelSystem levelSystem;
        public int ExperiencePerKill = 50;

        public void Initialize(LevelSystem levelSystem)
        {
            this.levelSystem = levelSystem;
        }

        private void OnEnemyKilled(PersonBehaviour enemy)
        {
            Destroy(enemy.transform.root);
            levelSystem.AddExperience(ExperiencePerKill);
        }

        public void OnUpdate()
        {
            
        }
    }



    public class TimeExperienceGainer : MonoBehaviour, IExperienceGainer
    {
        private LevelSystem levelSystem;
        public int ExperiencePerInterval = 10;
        public float IntervalDuration = 5f;
        private float timer;

        public void Initialize(LevelSystem levelSystem)
        {
            this.levelSystem = levelSystem;
        }

        public void OnUpdate()
        {
            timer += Time.deltaTime;
            if (timer >= IntervalDuration)
            {
                timer -= IntervalDuration;
                levelSystem.AddExperience(ExperiencePerInterval);
            }
        }
    }

    /*
    public class SoulEatingExperienceGainer : MonoBehaviour, IExperienceGainer
    {
        private LevelSystem levelSystem;
        public int ExperiencePerSoul = 100;

        public void Initialize(LevelSystem levelSystem)
        {
            this.levelSystem = levelSystem;
            // Subscribe to soul-eating events
            var soulEater = GetComponent<SoulEater>();
            if (soulEater != null)
            {
                soulEater.OnSoulEaten += OnSoulEaten;
            }
        }

        private void OnSoulEaten(Soul soul)
        {
            levelSystem.AddExperience(ExperiencePerSoul);
        }

        public void OnUpdate()
        {
            // No regular update needed for soul-eating experience
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            var soulEater = GetComponent<SoulEater>();
            if (soulEater != null)
            {
                soulEater.OnSoulEaten -= OnSoulEaten;
            }
        }
    }*/



}
