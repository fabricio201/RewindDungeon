﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Actor : MonoBehaviour {

	public enum Action {
		MOVE_U,
		MOVE_D,
		MOVE_L,
		MOVE_R,
        SHOOT
	}

    public GameObject selectionArrow;

	public float movementTime = 0.5f;
	public float rotationTime = 0.2f;

	public int initR;
	public int initC;

	public int r;
	public int c;

	public bool ready;
	public bool done;

    public bool isArcher;
    public bool isWarrior;

    public bool isAttacking;

    public Animator animController;

    public GameObject arrow;

	public List<Action> plan;

    public int maxActions;
    public int actionIndex;
    Action lastAction;

	private Board board;

    public ActorInfo info;

    public GameObject preview;

    ActorSfxManager sfxm;

	public void Spawn(Board board, int initR, int initC) {
		plan = new List<Action>();

        animController = GetComponent<Animator>();

		this.board = board;
		this.r = initR;
		this.c = initC;
		this.initR = initR;
		this.initC = initC;
		board.Set(initR, initC, gameObject);

		transform.position = board.GetCoordinates(initR, initC);

        if(GetComponent<PlayerCharacter>() != null && preview == null)
        {
            preview = Instantiate(GameManager.GM.PreviewToSpawn, transform.position + new Vector3(0, 0.1f, 0), GameManager.GM.PreviewToSpawn.transform.rotation);
            ShowPreview();
        }

        sfxm = GetComponent<ActorSfxManager>();
	}

	void OnDestroy() {
		Destroy(preview);
	}

	public void BeginPlan() {
        if (isArcher)
        {
            lastAction = Action.MOVE_U;
            AddAction(Action.SHOOT);
        }
        if (plan.Count > 0) {
            actionIndex = -1;
			ready = true;
			done = false;
		} else {
            actionIndex = -1;
			ready = false;
			done = true;
		}


	}

	public void Restart() {
		this.r = this.initR;
		this.c = this.initC;
		board.Set(r, c, gameObject);
		transform.position = board.GetCoordinates(r, c);
		this.SetReady();
	}

	public void AddAction(Action a) {
        if (plan.Count < maxActions)
        {
            actionIndex = -1;
            plan.Add(a);

            if(a != Action.SHOOT)
            {
                UpdatePreview();
            }
        }
        else
        {
            //cannot add any more actions
        }
	}

	public void ClearActions() {
		plan.Clear();
	}

    void UpdatePreview()
    {
        int nr, nc;

        if (preview == null) {
        	return;
        }
        nr = r;
        nc = c;
        for(int i = 0; i < plan.Count; i++)
        {
            NextPos(nr, nc, plan[i], out nr, out nc);
        }

        preview.transform.position = board.GetCoordinates(nr, nc) + new Vector3(0, 0.15f, 0);
    }

    public void ShowPreview() {
        if (preview != null) {
            preview.SetActive(true);
        }
    }

    public void HidePreview()
    {
        if (preview != null) {
            preview.SetActive(false);
        }
    }

    public bool NextAction(bool force=false) {
		if (done || !force && !ready && !isAttacking) {
			return false;
		}
        actionIndex++;
        if (actionIndex >= plan.Count)
        {
            done = true;
            return false;
        }
        if (actionIndex > 0)
        {
            lastAction = plan[actionIndex-1];
        }
		return true;
	}

	public bool PerformAction() {
		int nr, nc;
        switch (plan[actionIndex]) {
			case Action.MOVE_U:
			case Action.MOVE_D:
			case Action.MOVE_L:
			case Action.MOVE_R:
				NextPos(out nr, out nc);
				return TryMoveTo(nr, nc);
            case Action.SHOOT:
                ready = false;
                animController.SetTrigger("Shoot");
                NextPos(lastAction, out nr, out nc);
                if (board.WithinBounds(nr, nc)) {
                    GameObject obj = Instantiate(arrow, board.GetCoordinates(nr, nc), arrow.transform.rotation);
                    obj.transform.eulerAngles = new Vector3(obj.transform.eulerAngles.x, transform.eulerAngles.y-180, obj.transform.eulerAngles.z);
                    Arrow arrowScript = obj.GetComponent<Arrow>();
                    arrowScript.archerParent = this;
                    obj.GetComponent<Rigidbody>().velocity = -obj.transform.up * arrowScript.speed;
                }
                break;
        }
		return false;
	}


	private bool TryMoveTo(int nr, int nc) {
		if (board.Move(r, c, nr, nc)) {
			r = nr;
			c = nc;
			AnimateMovement();
			return true;
		}
        else if (board.WithinBounds(nr, nc))
        {
            GameObject obj = GameManager.GM.board.Get(nr, nc);
            if (this.CompareTag("Enemy") && obj.CompareTag("Player"))
            {
            	StartCoroutine(Attack(obj.GetComponent<Actor>()));
            	return true;
            } else if (this.CompareTag("Player")
            		&& obj.CompareTag("Enemy")
            		&& isWarrior) {
            	StartCoroutine(Attack(obj.GetComponent<Actor>()));
            	return true;
            }
        }
        return false;
    }

    private bool NextPos(int r, int c, Action action, out int nr, out int nc)
    {
        nr = r;
        nc = c;
        switch (action)
        {
            case Action.MOVE_U:
                nr = r - 1;
                return true;
            case Action.MOVE_D:
                nr = r + 1;
                return true;
            case Action.MOVE_L:
                nc = c - 1;
                return true;
            case Action.MOVE_R:
                nc = c + 1;
                return true;
        }
        return false;
    }

	private bool NextPos(out int nr, out int nc) {
        return NextPos(plan[actionIndex], out nr, out nc);
	}

    private bool NextPos(Action action, out int nr, out int nc)
    {
        return NextPos(r, c, action, out nr, out nc);
    }

    public void LookAtTargetPos() {
		int nr, nc;
		if (NextPos(out nr, out nc)) {
			ready = false;
			Vector3 pos = board.GetCoordinates(nr, nc);
			iTween.LookTo(
				gameObject,
				iTween.Hash(
					"looktarget", pos,
					"axis", "y",
					"delay", 0,
					"time", rotationTime,
					"oncomplete", "SetReady"));
		}
	}

	private void AnimateMovement() {
		ready = false;
		Vector3 pos = board.GetCoordinates(r, c);
		iTween.LookTo(
			gameObject,
			iTween.Hash(
				"looktarget", pos,
				"axis", "y",
				"delay", 0,
				"time", rotationTime,
				"oncomplete", "EndTurning"));
	}

	void EndTurning() {
		Vector3 pos = board.GetCoordinates(r, c);
        iTween.MoveTo(
			gameObject,
			iTween.Hash(
				"x", pos.x,
				"z", pos.z,
				"easetype", "easeOutQuad",
				"orienttopath", true,
				"delay", 0,
				"time", movementTime,
                "name", "movement",
                "onstart", "TriggerRun",
				"oncomplete", "EndMoving"));
	}

    void TriggerRun()
    {
        animController.SetTrigger("Run");
    }

    void EndMoving()
    {
        if (isArcher && actionIndex < plan.Count-1 && plan[actionIndex+1] == Action.SHOOT)
        {
            NextAction(true);
            PerformAction();
        } else {
            SetReady();
        }
    }

	public void SetReady() {
		ready = true;
	}

	private IEnumerator Attack(Actor target) {
        animController.SetTrigger("Attack");
		ready = false;
		target.ready = false;
		isAttacking = true;
        if (isWarrior) {
            yield return new WaitForSeconds(2.0f);
        } else if (target.isWarrior) {
            yield return new WaitForSeconds(0.5f);
        } else {
            yield return new WaitForSeconds(1.0f);
        }
        sfxm.PlayAttackSound();
		target.TakeDamage(this);
	}

	public void PostAttack(Actor target) {
		int nr = target.r;
		int nc = target.c;

		isAttacking = false;
		if (board.Move(r, c, nr, nc)) {
            r = nr;
            c = nc;
            AnimateMovement();
        }
	}

    public void TakeDamage(Actor src) {
        if (isWarrior){
            Block(src);
        } else {
            Die();
            src.PostAttack(this);
        }
    }

    void Block(Actor dmgSrc)
    {
        iTween.LookTo(gameObject, dmgSrc.transform.position, 0.1f);
        animController.SetTrigger("Block");
        StartCoroutine(Attack(dmgSrc));
    }

    private void Die()
    {
        sfxm.PlayDeathSound();
        animController.SetTrigger("Die");
        done = true;
        board.Set(r, c, null);
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("Button")) {
            col.gameObject.GetComponent<ButtonBehaviour>().Trigger();
        }
    }


}
