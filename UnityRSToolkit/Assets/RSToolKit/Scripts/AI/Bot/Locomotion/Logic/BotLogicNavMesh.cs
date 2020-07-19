﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RSToolkit.Helpers;
using RSToolkit.AI.Helpers;
using RSToolkit.Space3D;
using RSToolkit.AI.FSM;

namespace RSToolkit.AI.Locomotion
{

    public class BotLogicNavMesh : BotLogicLocomotion
    {
        public float WalkSpeed { get; set; } 
        public float WalkRotationSpeed { get; set; } 

        public float RunSpeed { get; set; }
        public float RunRotationSpeed { get; set; }

        #region Components

        public NavMeshAgent NavMeshAgentComponent { get; private set; }

        #endregion Components

        public ProximityChecker JumpProximityChecker;

        public override float CurrentSpeed
        {
            get
            {
                return NavMeshHelpers.GetCurrentSpeed(NavMeshAgentComponent);
            }
        }

        public override void RotateTowardsPosition()
        {
            var rotation = Quaternion.LookRotation(BotLocomotiveComponent.FocusedOnPosition.Value - BotLocomotiveComponent.transform.position, Vector3.up);
            BotLocomotiveComponent.transform.rotation = Quaternion.RotateTowards(BotLocomotiveComponent.transform.rotation, rotation, NavMeshAgentComponent.angularSpeed * Time.deltaTime);
        }

        public override void RotateAwayFromPosition()
        {
            var rotation = Quaternion.LookRotation(BotLocomotiveComponent.GetMoveAwayDestination() - BotLocomotiveComponent.transform.position, Vector3.up);
            BotLocomotiveComponent.transform.rotation = Quaternion.RotateTowards(BotLocomotiveComponent.transform.rotation, rotation, NavMeshAgentComponent.angularSpeed * Time.deltaTime);
        }

        private void MoveTo(Vector3 destination, float speed, float angularSpeed)
        {
                NavMeshAgentComponent.speed = speed;
                NavMeshAgentComponent.angularSpeed = angularSpeed;
                NavMeshAgentComponent.destination = destination;
                NavMeshAgentComponent.stoppingDistance = 0f;
                switch (BotLocomotiveComponent.StopMovementCondition)
                {
                    case BotLocomotive.StopMovementConditions.WITHIN_INTERACTION_DISTANCE:
                        NavMeshAgentComponent.stoppingDistance = BotLocomotiveComponent.SqrInteractionMagnitude * .75f;
                        break;
                    case BotLocomotive.StopMovementConditions.WITHIN_PERSONAL_SPACE:
                        NavMeshAgentComponent.stoppingDistance = BotLocomotiveComponent.SqrPersonalSpaceMagnitude * .75f;
                        break;
                }
                NavMeshAgentComponent.isStopped = false;          
        }

        public override void MoveTowardsPosition(bool fullspeed = true)
        {
            if (fullspeed)
            {
                MoveTo(BotLocomotiveComponent.FocusedOnPosition.Value, RunSpeed, RunRotationSpeed);
            }
            else
            {
                MoveTo(BotLocomotiveComponent.FocusedOnPosition.Value, WalkSpeed, WalkRotationSpeed);
            }
        }

        public override void MoveAway(bool fullspeed = true)
        {

            if (fullspeed)
            {
                MoveTo(BotLocomotiveComponent.GetMoveAwayDestination(), RunSpeed, RunRotationSpeed);
            }
            else
            {
                MoveTo(BotLocomotiveComponent.GetMoveAwayDestination(), WalkSpeed, WalkRotationSpeed);
            }
        }

        public void MoveToClosestEdge(bool fullspeed = true)
        {
            NavMeshHit hit;
            NavMeshAgentComponent.FindClosestEdge(out hit);
            BotLocomotiveComponent.UnFocus();
            BotLocomotiveComponent.FocusOnPosition(hit.position);
            MoveTowardsPosition(fullspeed);
            BotLocomotiveComponent.MoveToPosition(BotLocomotive.StopMovementConditions.WITHIN_PERSONAL_SPACE, fullspeed);
        }

        public Vector3? JumpOffLedge(bool fullspeed = false)
        {
            RaycastHit rayhit;

            if (CanJumpDown(out rayhit))
            {
                BotLocomotiveComponent.UnFocus();
                BotLocomotiveComponent.FocusOnPosition(rayhit.point);
                BotLocomotiveComponent.MoveToPosition(BotLocomotive.StopMovementConditions.AT_POSITION, fullspeed);
                return rayhit.point;
            }
            return null;
        }

        public bool CanJumpDown(out RaycastHit rayhit)
        {
            if (JumpProximityChecker.IsWithinRayDistance(out rayhit) != null)
            {
                var jumpPath = new NavMeshPath();
                NavMesh.CalculatePath(BotLocomotiveComponent.transform.position, rayhit.point, NavMesh.AllAreas, jumpPath);
                return jumpPath.status == NavMeshPathStatus.PathComplete;
            }
            return false;
        }


        protected virtual void OffMeshLinkUpdate(NavMeshHelpers.OffMeshLinkPosition linkposition)
        {

        }

        private NavMeshHelpers.OffMeshLinkPosition m_linkposition;
        private void CheckUpdateOffMeshLinkPosition()
        {
            m_linkposition = NavMeshAgentComponent.GetOffMeshLinkPosition();
            if (m_linkposition != NavMeshHelpers.OffMeshLinkPosition.Off)
            {
                OffMeshLinkUpdate(m_linkposition);
            }
        }

        private void CheckLinkArea()
        {

        }

        public override bool CanMove()
        {
            return (NavMeshAgentComponent.speed > 0
                && NavMeshAgentComponent.angularSpeed > 0
                && NavMeshAgentComponent.isActiveAndEnabled);
        }

        public bool IsAboveNavMeshSurface()
        {
            return NavMeshAgentComponent.IsAboveNavMeshSurface();
        }


        #region States

        public override void OnStateChange(BotLocomotive.LocomotionStates locomotionState)
        {
            switch (locomotionState)
            {
                case BotLocomotive.LocomotionStates.NotMoving:
                    if (NavMeshAgentComponent.isOnNavMesh)
                    {
                        NavMeshAgentComponent.isStopped = true;
                    }
                    break;
            }
        }

        #endregion States

        public BotLogicNavMesh(BotLocomotive botLocomotion, NavMeshAgent navMeshAgentComponent,
            float walkSpeed = 0.75f, float walkRotationSpeed = 120f,
            float runSpeed = 5f, float runRotationSpeed = 120f) : base(botLocomotion)
        {
            NavMeshAgentComponent = navMeshAgentComponent;

            WalkSpeed = walkSpeed;
            WalkRotationSpeed = walkRotationSpeed;
            RunSpeed = runSpeed;
            RunRotationSpeed = runRotationSpeed;

            NavMeshAgentComponent.speed = WalkSpeed;
            NavMeshAgentComponent.angularSpeed = WalkRotationSpeed;

        }

    }
}

