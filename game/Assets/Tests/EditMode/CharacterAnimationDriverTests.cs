using NUnit.Framework;
using UnityEngine;

public class CharacterAnimationDriverTests
{
    [Test]
    public void TryPlay_WhenNoAnimatorExists_ReturnsFalse()
    {
        GameObject character = new GameObject("Character");
        try
        {
            CharacterAnimationDriver driver = character.AddComponent<CharacterAnimationDriver>();

            Assert.IsFalse(driver.TryPlay("Walk"));
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
    }

    [Test]
    public void Configure_SetsAnimatorWhenPresent()
    {
        GameObject character = new GameObject("Character");
        try
        {
            Animator animator = character.AddComponent<Animator>();
            CharacterAnimationDriver driver = character.AddComponent<CharacterAnimationDriver>();

            driver.Configure(animator);

            Assert.IsTrue(driver.HasAnimator);
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
    }
}
