using UnityEngine;

public sealed class ArtisanVisual : MonoBehaviour
{
    Graybox game;
    int player;
    Animator animator;
    bool speed, carrying, working;
    public void Initialize(Graybox owner, int index, RuntimeAnimatorController controller)
    {
        game = owner;
        player = index;
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
            return;
        if (controller != null)
            animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        foreach (var p in animator.parameters)
        {
            speed |= p.name == "Speed" && p.type == AnimatorControllerParameterType.Float;
            carrying |= p.name == "Carrying" && p.type == AnimatorControllerParameterType.Bool;
            working |= p.name == "Working" && p.type == AnimatorControllerParameterType.Bool;
        }
    }

    void Update()
    {
        if (animator == null || game == null)
            return;
        bool active = game.Flow.WorldActive && game.Rules.Active;
        if (speed)
            animator.SetFloat("Speed", active ? game.Controls.Move(player).magnitude : 0);
        if (carrying)
            animator.SetBool("Carrying", game.Rules.Hands[player] != null);
        if (working)
            animator.SetBool("Working", active && game.Controls.Work(player) && game.Players[player].Target >= 0);
        animator.speed = active ? 1 : 0;
    }
}
