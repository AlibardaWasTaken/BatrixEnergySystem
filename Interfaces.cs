using UnityEngine;

namespace ENSYS
{
    public interface INullable
    {
        void Nullify();
    }




    public interface IDescriptable
    {
        string DescTitle();

        string Description();

        Sprite DescImage();
    }



    public interface ILevelingStrategy
    {
        int GetExperienceToNextLevel(int currentLevel);
    }

    public interface IExperienceGainer
    {
        void Initialize(LevelSystem levelSystem);
        void OnUpdate(); // Called every frame or at specific intervals
    }

}
