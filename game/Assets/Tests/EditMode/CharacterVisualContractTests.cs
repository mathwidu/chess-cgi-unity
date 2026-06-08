using NUnit.Framework;
using UnityEngine;

public class CharacterVisualContractTests
{
    [Test]
    public void Configure_StoresPieceKindRigStatusAndAnimator()
    {
        GameObject character = new GameObject("Character");
        try
        {
            Animator animator = character.AddComponent<Animator>();
            CharacterVisualContract contract = character.AddComponent<CharacterVisualContract>();

            contract.Configure(ChessPieceKind.Pawn, CharacterRigStatus.RiggedHumanoid, animator);

            Assert.AreEqual(ChessPieceKind.Pawn, contract.PieceKind);
            Assert.AreEqual(CharacterRigStatus.RiggedHumanoid, contract.RigStatus);
            Assert.AreSame(animator, contract.Animator);
            Assert.IsTrue(contract.HasAnimator);
            Assert.IsTrue(contract.IsRigged);
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
    }

    [Test]
    public void EnsureRequiredSockets_CreatesStableAnimationSockets()
    {
        GameObject character = new GameObject("Character");
        try
        {
            CharacterVisualContract contract = character.AddComponent<CharacterVisualContract>();

            contract.Configure(ChessPieceKind.Queen, CharacterRigStatus.StaticMesh, null);
            contract.EnsureRequiredSockets();

            Assert.IsNotNull(contract.EffectsSocket);
            Assert.IsNotNull(contract.HitSocket);
            Assert.IsNotNull(contract.GroundSocket);
            Assert.IsNotNull(contract.WeaponSocket);
            Assert.IsNotNull(contract.RightHandSocket);
            Assert.IsNotNull(contract.LeftHandSocket);
            Assert.IsNotNull(contract.CastSocket);
            Assert.AreEqual("EffectsSocket", contract.EffectsSocket.name);
            Assert.AreEqual("HitSocket", contract.HitSocket.name);
            Assert.AreEqual("GroundSocket", contract.GroundSocket.name);
            Assert.AreEqual("WeaponSocket", contract.WeaponSocket.name);
            Assert.AreEqual("RightHandSocket", contract.RightHandSocket.name);
            Assert.AreEqual("LeftHandSocket", contract.LeftHandSocket.name);
            Assert.AreEqual("CastSocket", contract.CastSocket.name);
            Assert.AreEqual(Vector3.up * 0.75f, contract.HitSocket.localPosition);
            Assert.AreEqual(new Vector3(0.28f, 0.72f, 0.12f), contract.WeaponSocket.localPosition);
            Assert.AreEqual(new Vector3(0.34f, 0.62f, 0.04f), contract.RightHandSocket.localPosition);
            Assert.AreEqual(new Vector3(-0.34f, 0.62f, 0.04f), contract.LeftHandSocket.localPosition);
            Assert.AreEqual(new Vector3(0f, 0.92f, 0.22f), contract.CastSocket.localPosition);
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
    }
}
