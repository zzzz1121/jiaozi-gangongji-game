using UnityEngine;

public sealed class ArtisanVisual : MonoBehaviour
{
    Graybox game;
    int player;
    Animator animator;
    Animation modelAnimation;
    Transform holdPoint;
    string playing;
    bool speed, carrying, working;
    public void Initialize(Graybox owner, int index, RuntimeAnimatorController controller)
    {
        game = owner;
        player = index;
        modelAnimation = GetComponentInChildren<Animation>();
        holdPoint = Graybox.FindModelNode(transform, "HoldPoint");
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
        if (game == null || game.Flow == null)
            return;
        bool active = game.Flow.WorldActive && game.Rules.Active;
        if (modelAnimation != null)
        {
            bool moves = game.Controls.Move(player).sqrMagnitude > .01f;
            bool holds = game.Rules.Hands[player] != null;
            bool works = game.Controls.Work(player) && game.Players[player].Target >= 0;
            string suffix = works ? "_Work" : holds ? (moves ? "_CarryWalk" : "_CarryIdle") : moves ? "_Walk" : "_Idle";
            foreach (AnimationState state in modelAnimation)
            {
                state.speed = active ? 1 : 0;
                if (!state.name.EndsWith(suffix, System.StringComparison.Ordinal) || playing == state.name || !active)
                    continue;
                state.wrapMode = WrapMode.Loop;
                playing = state.name;
                modelAnimation.CrossFade(playing, .12f);
            }
        }
        if (animator == null) return;
        if (speed)
            animator.SetFloat("Speed", active ? game.Controls.Move(player).magnitude : 0);
        if (carrying)
            animator.SetBool("Carrying", game.Rules.Hands[player] != null);
        if (working)
            animator.SetBool("Working", active && game.Controls.Work(player) && game.Players[player].Target >= 0);
        animator.speed = active ? 1 : 0;
    }

    void LateUpdate()
    {
        if (holdPoint == null || game == null) return;
        game.Players[player].Cargo.position = holdPoint.position;
    }
}
