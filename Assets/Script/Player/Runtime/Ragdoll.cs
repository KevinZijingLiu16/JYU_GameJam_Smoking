using System.Collections.Generic;
using UnityEngine;

namespace Smoking.Player
{
    // Builds a simple ragdoll from a Humanoid Animator's bones at startup. The physics stays
    // off (kinematic, colliders disabled) while animated, so it never blocks the ground probe.
    [RequireComponent(typeof(Animator))]
    public sealed class Ragdoll : MonoBehaviour
    {
        [SerializeField] private float totalMass = 20f;

        private readonly List<Rigidbody> bodies = new List<Rigidbody>();
        private readonly List<Collider> colliders = new List<Collider>();
        private Animator animator;
        private float height;
        private Vector3 right, forward;

        public bool IsActive { get; private set; }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            Build();
            SetPhysics(false);
        }

        public void Activate(Vector3 velocity)
        {
            if (IsActive) return;
            IsActive = true;
            animator.enabled = false;
            SetPhysics(true);
            foreach (var body in bodies) body.linearVelocity = velocity;
        }

        private void SetPhysics(bool on)
        {
            foreach (var body in bodies) body.isKinematic = !on;
            foreach (var col in colliders) col.enabled = on;
        }

        private Transform Bone(HumanBodyBones bone) => animator.GetBoneTransform(bone);

        private void Build()
        {
            Transform hips = Bone(HumanBodyBones.Hips), spine = Bone(HumanBodyBones.Spine), head = Bone(HumanBodyBones.Head);
            Transform neck = Bone(HumanBodyBones.Neck) != null ? Bone(HumanBodyBones.Neck) : head;
            Transform lUpLeg = Bone(HumanBodyBones.LeftUpperLeg), rUpLeg = Bone(HumanBodyBones.RightUpperLeg);
            height = Vector3.Distance(head.position, Bone(HumanBodyBones.LeftFoot).position) / 0.9f;
            right = transform.right;
            forward = transform.forward;

            var hipsBody = AddBody(hips, 0.2f);
            AddSphere(hips, (lUpLeg.position + rUpLeg.position + hips.position) / 3f, 0.09f * height);

            var spineBody = AddBody(spine, 0.2f);
            AddJoint(spine, hipsBody, right, -20f, 20f, 10f);
            AddCapsule(spine, neck.position, 0.1f * height);

            AddBody(head, 0.08f);
            AddJoint(head, spineBody, right, -40f, 25f, 25f);
            Vector3 up = (head.position - neck.position).sqrMagnitude > 0.0001f ? (head.position - neck.position).normalized : transform.up;
            AddSphere(head, head.position + up * 0.06f * height, 0.075f * height);

            AddLimb(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, spineBody, transform.up, 0.035f, -70f, 10f, 50f, -90f);
            AddLimb(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, spineBody, transform.up, 0.035f, -70f, 10f, 50f, -90f);
            AddLimb(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, hipsBody, right, 0.05f, -20f, 70f, 30f, -80f);
            AddLimb(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, hipsBody, right, 0.05f, -20f, 70f, 30f, -80f);
        }

        private void AddLimb(HumanBodyBones upperBone, HumanBodyBones lowerBone, HumanBodyBones endBone, Rigidbody parent,
            Vector3 bendAxis, float radius, float low, float high, float swing, float bend)
        {
            Transform upper = Bone(upperBone), lower = Bone(lowerBone), end = Bone(endBone);
            var upperBody = AddBody(upper, 0.06f);
            AddJoint(upper, parent, bendAxis, low, high, swing);
            AddCapsule(upper, lower.position, radius * height);

            AddBody(lower, 0.04f);
            AddJoint(lower, upperBody, bendAxis, bend, 0f, 0f);
            AddCapsule(lower, end.position, radius * 0.8f * height);
        }

        private Rigidbody AddBody(Transform bone, float massShare)
        {
            var body = bone.gameObject.AddComponent<Rigidbody>();
            body.mass = totalMass * massShare;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            bodies.Add(body);
            return body;
        }

        private void AddJoint(Transform bone, Rigidbody parent, Vector3 worldAxis, float low, float high, float swing)
        {
            var joint = bone.gameObject.AddComponent<CharacterJoint>();
            joint.connectedBody = parent;
            joint.axis = bone.InverseTransformDirection(worldAxis);
            joint.swingAxis = bone.InverseTransformDirection(forward);
            joint.lowTwistLimit = new SoftJointLimit { limit = low };
            joint.highTwistLimit = new SoftJointLimit { limit = high };
            joint.swing1Limit = new SoftJointLimit { limit = swing };
            joint.swing2Limit = new SoftJointLimit { limit = swing };
            joint.enableProjection = true;
            joint.enablePreprocessing = false;
        }

        private void AddCapsule(Transform bone, Vector3 endWorld, float radiusWorld)
        {
            Vector3 local = bone.InverseTransformPoint(endWorld);
            float scale = bone.lossyScale.x;
            var capsule = bone.gameObject.AddComponent<CapsuleCollider>();
            Vector3 abs = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
            capsule.direction = abs.x > abs.y && abs.x > abs.z ? 0 : abs.y > abs.z ? 1 : 2;
            capsule.center = local * 0.5f;
            capsule.height = local.magnitude;
            capsule.radius = radiusWorld / scale;
            colliders.Add(capsule);
        }

        private void AddSphere(Transform bone, Vector3 centerWorld, float radiusWorld)
        {
            var sphere = bone.gameObject.AddComponent<SphereCollider>();
            sphere.center = bone.InverseTransformPoint(centerWorld);
            sphere.radius = radiusWorld / bone.lossyScale.x;
            colliders.Add(sphere);
        }
    }
}
