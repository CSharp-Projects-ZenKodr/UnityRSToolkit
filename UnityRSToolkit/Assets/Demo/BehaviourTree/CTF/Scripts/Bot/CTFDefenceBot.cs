﻿using UnityEngine;
using RSToolkit.AI.Behaviour;
using RSToolkit.AI.Behaviour.Composite;
using RSToolkit.AI.Behaviour.Task;
using RSToolkit.AI.Behaviour.Decorator;
using RSToolkit.AI.Locomotion;
using RSToolkit.Helpers;
using System;

namespace Demo.BehaviourTree.CTF{
    public class CTFDefenceBot : CTFBot
    {
        [SerializeField]
        private Transform[] m_waypoints;
        private int m_waypointIndex = 0;

        public CTFBot TargetEnemyBot {get; private set;}

        public bool IsWithinDefenceRadius(){
            if(_botLocomotiveComponent.FocusedOnPosition == null){
                return false;
            }
            var thisPosition = transform.position;
            thisPosition.y = _botLocomotiveComponent.FocusedOnPosition.Value.y;
            return Vector3.SqrMagnitude(thisPosition - _botLocomotiveComponent.FocusedOnPosition.Value) <= _botLocomotiveComponent.SqrAwarenessMagnitude;
        }

        public bool IsFocused(){
            return _botLocomotiveComponent.IsFocused && TargetEnemyBot != null;
        }

#region Behaviour Structs

        public struct DefendFlagNotTakenBehaviours{
            public BehaviourRootNode Root;
            public BehaviourSelector MainSelector;
            public BehaviourAction DoSeekStartingPosition ;
            public BotIsWithinSight IsEnemyWithinSight;
            public BehaviourAction DoDefend;
        }

        public struct PatrolFlagNotTakenBehaviours{
            public BehaviourRootNode Root;
            public BehaviourSequence PatrolSequence;
            public BotDoPatrol DoPatrol;
            public BotDoSeek DoSeekEnemy;
        }

        public struct DefendFlagTakenBehaviours{
            public BehaviourRootNode Root;
            public BehaviourCondition IsFlagNotCaptured;
            public BotDoSeek DoSeekEnemy;
        }

#endregion Behaviour Structs

#region Init Behaviours
        public DefendFlagNotTakenBehaviours CTFDefend_FlagNotTakenBehaviours; 
        public DefendFlagTakenBehaviours CTFDefend_FlagTakenBehaviours; 
        public PatrolFlagNotTakenBehaviours CTFPatrol_FlagNotTakenBehaviours; 

        protected override void InitFlagNotTakenBehaviours(){
            CTFDefend_FlagNotTakenBehaviours.Root = new BehaviourRootNode("Flag Not Taken");
            CTFDefend_FlagNotTakenBehaviours.MainSelector = new BehaviourSelector(false);

            CTFDefend_FlagNotTakenBehaviours.DoDefend = new BehaviourAction(DoDefend, "Do Defend");
            CTFDefend_FlagNotTakenBehaviours.DoDefend.OnStarted.AddListener(DoDefendOnStarted_Listener);
            CTFDefend_FlagNotTakenBehaviours.IsEnemyWithinSight = new BotIsWithinSight(_botVisionComponent, CTFGameManager.TAG_OFFENCE, CTFDefend_FlagNotTakenBehaviours.DoDefend);
            CTFDefend_FlagNotTakenBehaviours.MainSelector.AddChild(CTFDefend_FlagNotTakenBehaviours.IsEnemyWithinSight);

            CTFDefend_FlagNotTakenBehaviours.DoSeekStartingPosition = new BehaviourAction(DoSeek, "Do Seek");
        }

        protected override void InitFlagTakenBehaviours(){
            if(m_waypoints.Length > 0){
                InitPatrolFlagTakenBehaviours();
            }else{
                InitDefendFlagTakenBehaviours();
            }
        }
        protected void InitDefendFlagTakenBehaviours(){
            CTFDefend_FlagTakenBehaviours.DoSeekEnemy = new BotDoSeek(_botLocomotiveComponent, true, "Do Seek Enemy");
            CTFDefend_FlagTakenBehaviours.IsFlagNotCaptured = new BehaviourCondition(IsFlagNotCapturedCondition, CTFDefend_FlagTakenBehaviours.DoSeekEnemy);
        }

        protected void InitPatrolFlagTakenBehaviours(){
            CTFPatrol_FlagNotTakenBehaviours.PatrolSequence = new BehaviourSequence(false);
            CTFPatrol_FlagNotTakenBehaviours.DoPatrol = new BotDoPatrol(_botLocomotiveComponent, CTFGameManager.TAG_OFFENCE);
			CTFPatrol_FlagNotTakenBehaviours.DoPatrol.OnStopped.AddListener(DoPatrol_OnStopped);
            CTFPatrol_FlagNotTakenBehaviours.DoSeekEnemy = new BotDoSeek(_botLocomotiveComponent, true, "Do Seek Action");
        }
#endregion Init Behaviours

        protected override BehaviourRootNode GetDefaultTree(){
            if(m_waypoints.Length > 0){
                return CTFPatrol_FlagNotTakenBehaviours.Root;
            }else{
                return CTFDefend_FlagNotTakenBehaviours.Root;
            }
        }
        public override void SwitchToTree_FlagTaken(){
            _behaviourManagerComponent.SetCurrentTree(CTFDefend_FlagTakenBehaviours.Root, true);
        }

        public override void SwitchToTree_FlagNotTaken(){
            if(m_waypoints.Length > 0){
                _behaviourManagerComponent.SetCurrentTree(CTFPatrol_FlagNotTakenBehaviours.Root, true);
            }else{
                _behaviourManagerComponent.SetCurrentTree(CTFDefend_FlagNotTakenBehaviours.Root, true);
            }
        }

#region Behaviour Logic
        protected bool IsFlagNotCapturedCondition(){
            return false;
        }
        
#region DoDefend
        private void DoDefendOnStarted_Listener(){
            SeekBot();
        }
        private BehaviourAction.ActionResult DoDefend(bool cancel)
        {
            if(cancel || !IsFocused() ||!IsWithinDefenceRadius()){
                _botLocomotiveComponent.StopMoving();
                return BehaviourAction.ActionResult.FAILED;
            }

            if(_botLocomotiveComponent.IsWithinInteractionDistance()){
                return BehaviourAction.ActionResult.SUCCESS;
            }

            return BehaviourAction.ActionResult.PROGRESS;
        }
#endregion DoDefend
#region DoPatrol
		private void  DoPatrol_OnStopped(bool success){
			TargetEnemyBot = CTFPatrol_FlagNotTakenBehaviours.DoPatrol.TargetTransforms[0].GetComponent<CTFBot>(); 
		}
#endregion DoPatrol

        private void MoveToWaypoint(){
            _botLocomotiveComponent.FocusOnTransform(m_waypoints[m_waypointIndex]);
            _botLocomotiveComponent.MoveToTarget(BotLocomotive.StopMovementConditions.WITHIN_PERSONAL_SPACE, false);
        }


        private void MoveToNextWaypoint(){
            m_waypointIndex = CollectionHelpers.GetNextCircularIndex(m_waypointIndex, m_waypoints.Length);
            MoveToWaypoint();
        }

#endregion Behaviour Logic

        private void SeekBot(){
            TargetEnemyBot = _botVisionComponent.DoLookoutFor(false, false, CTFGameManager.TAG_OFFENCE)[0].GetComponent<CTFBot>();
            TargetEnemyBot.OnDie.AddListener(OnTargetDie_Listener); 
            _botLocomotiveComponent.MoveToTarget(BotLocomotive.StopMovementConditions.WITHIN_PERSONAL_SPACE);
        }

        #region Events
        public void OnTargetDie_Listener(CTFBot target){
           target.OnDie.RemoveListener(OnTargetDie_Listener);
           if(target == TargetEnemyBot){
               TargetEnemyBot = null;
               _botLocomotiveComponent.UnFocus();
           }
        }

        #endregion Events

        #region Mono Functions
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        #endregion Mono Functions
    }
}