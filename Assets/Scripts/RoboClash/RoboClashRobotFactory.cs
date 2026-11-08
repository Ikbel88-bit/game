using UnityEngine;

namespace RoboClash
{
    public static class RoboClashRobotFactory
    {
        public static GameObject CreateRobot(RoboClashRobotType type, Transform parent,
            Material primary, Material secondary, Material joints, Material emission)
        {
            return CreateRobot(type, parent, primary, secondary, joints, emission, null, null);
        }

        public static GameObject CreateRobot(RoboClashRobotType type, Transform parent,
            Material primary, Material secondary, Material joints, Material emission,
            Mesh armorMesh, Material shimmer)
        {
            var root = new GameObject(type.ToString().ToUpperInvariant());
            if (parent != null)
                root.transform.SetParent(parent, false);

            var dimensions = RoboClashRobotDefinition.GetTargetDimensions(type);
            var controller = root.AddComponent<CharacterController>();
            controller.height = dimensions.y;
            controller.radius = dimensions.x * 0.47f;
            controller.center = Vector3.up * (dimensions.y * 0.5f);
            controller.stepOffset = Mathf.Min(0.35f, dimensions.y * 0.18f);
            controller.slopeLimit = 50f;

            var definition = root.AddComponent<RoboClashRobotDefinition>();
            definition.Configure(type);
            definition.teamId = (int)type;
            definition.emissionMaterial = emission;
            definition.shimmerMaterial = shimmer;
            var player = root.AddComponent<RobotArenaController>();
            var movement = root.AddComponent<RoboClashRobotMovement>();
            var combat = root.AddComponent<RoboClashRobotCombat>();
            root.AddComponent<RoboClashRobotAI>();
            var abilities = root.AddComponent<RoboClashRobotAbilities>();
            var animator = root.AddComponent<RoboClashRobotAnimator>();
            var audio = root.AddComponent<RoboClashRobotAudio>();
            audio.Configure(type);
            var visualEffects = root.AddComponent<RoboClashRobotVFX>();

            var model = new GameObject("ModelRoot").transform;
            model.SetParent(root.transform, false);
            definition.modelRoot = model;
            definition.EnsureCameraAnchors();

            var silhouette = BuildSilhouette(type, model, dimensions, primary, secondary, joints, emission,
                armorMesh, shimmer);
            definition.aimPivot = silhouette.aimPivot;
            definition.weaponMount = silhouette.weaponMount;
            definition.muzzle = silhouette.muzzle;
            if (definition.muzzle == null)
                definition.muzzle = FindDescendant(silhouette.weaponMount, "Muzzle");
            definition.rotatingBarrels = silhouette.rotatingBarrels;
            definition.repairDrone = silhouette.repairDrone;

            movement.SetVisualParts(silhouette.leftLeg, silhouette.rightLeg, silhouette.torso);
            animator.SetVisualParts(model, silhouette.torso, silhouette.aimPivot,
                silhouette.leftLeg, silhouette.rightLeg, silhouette.leftArm, silhouette.rightArm);
            combat.SetVisualParts(silhouette.weaponMount, silhouette.muzzleFlash);
            visualEffects.ConfigureVisuals();
            abilities.ConfigureVisuals();
            player.Configure(null, silhouette.muzzle, null);
            movement.ConfigurePlayer(null, false);
            combat.ConfigurePlayer(false);
            return root;
        }

        private static RobotSilhouette BuildSilhouette(RoboClashRobotType type, Transform model,
            Vector3 dimensions, Material primary, Material secondary, Material joints, Material emission,
            Mesh armorMesh, Material shimmer)
        {
            var silhouette = new RobotSilhouette();
            var scale = dimensions.y / 2f;
            var halfWidth = dimensions.x * 0.5f;

            var torso = Child("Torso", model, new Vector3(0f, 0.95f * scale, 0f));
            silhouette.torso = torso;
            var torsoWidth = type == RoboClashRobotType.Titan ? 0.95f : 0.62f;
            var torsoDepth = type == RoboClashRobotType.Titan ? 0.66f : 0.48f;
            ArmorPiece(armorMesh, "Chest Armor Shell", torso, Vector3.zero,
                new Vector3(torsoWidth, 0.8f, torsoDepth), primary);
            ArmorPiece(armorMesh, "Upper Chest Plate", torso, new Vector3(0f, 0.22f, torsoDepth * 0.48f),
                new Vector3(torsoWidth * 0.88f, 0.34f, 0.12f), secondary);
            ArmorPiece(armorMesh, "Lower Chest Plate", torso, new Vector3(0f, -0.22f, torsoDepth * 0.48f),
                new Vector3(torsoWidth * 0.78f, 0.22f, 0.1f), primary);
            var coreSize = type == RoboClashRobotType.Titan ? 0.27f : 0.18f;
            Piece(PrimitiveType.Sphere, "Chest Reactor Housing", torso,
                new Vector3(0f, 0.02f, torsoDepth * 0.57f),
                new Vector3(coreSize * 1.65f, coreSize * 1.35f, 0.1f), joints);
            Piece(PrimitiveType.Sphere, "Chest Energy Core", torso,
                new Vector3(0f, 0.02f, torsoDepth * 0.68f),
                new Vector3(coreSize, coreSize, 0.075f), emission);

            for (var side = -1; side <= 1; side += 2)
            {
                ArmorPiece(armorMesh, side < 0 ? "Left Chest Chevron" : "Right Chest Chevron",
                    torso, new Vector3(side * torsoWidth * 0.34f, -0.01f, torsoDepth * 0.52f),
                    new Vector3(torsoWidth * 0.14f, 0.37f, 0.07f), secondary,
                    Quaternion.Euler(0f, 0f, side * -14f));
                for (var vent = 0; vent < 3; vent++)
                    ArmorPiece(armorMesh, "Chest Cooling Vent " + side + " " + vent, torso,
                        new Vector3(side * torsoWidth * 0.34f, 0.25f - vent * 0.065f, torsoDepth * 0.55f),
                        new Vector3(0.1f, 0.018f, 0.025f), joints);
            }

            var aimPivot = Child("AimPivot", model, new Vector3(0f, 1.65f * scale, 0f));
            silhouette.aimPivot = aimPivot;
            var headScale = type == RoboClashRobotType.Titan ? 0.39f : 0.3f;
            ArmorPiece(armorMesh, "Angular Helmet Shell", aimPivot, new Vector3(0f, 0.06f, 0f),
                new Vector3(headScale * 1.5f, headScale, headScale * 0.9f), primary);
            ArmorPiece(armorMesh, "Helmet Brow Armor", aimPivot,
                new Vector3(0f, headScale * 0.25f, headScale * 0.33f),
                new Vector3(headScale * 1.3f, 0.09f, 0.12f), secondary);
            ArmorPiece(armorMesh, "Visor Housing", aimPivot,
                new Vector3(0f, 0.035f, headScale * 0.47f),
                new Vector3(headScale * 1.23f, 0.13f, 0.07f), joints);
            ArmorPiece(armorMesh, "Optic Visor", aimPivot,
                new Vector3(0f, 0.035f, headScale * 0.54f),
                new Vector3(headScale * 0.94f, 0.045f, 0.025f), emission);
            for (var side = -1; side <= 1; side += 2)
            {
                Piece(PrimitiveType.Cylinder, "Helmet Sensor", aimPivot,
                    new Vector3(side * headScale * 0.73f, 0.04f, 0.01f),
                    new Vector3(0.055f, 0.1f, 0.055f), emission, Quaternion.Euler(90f, 0f, 0f));
            }

            var shoulderWidth = type == RoboClashRobotType.Titan ? 0.48f : 0.3f;
            var armLength = type == RoboClashRobotType.Titan ? 0.58f : 0.48f;
            silhouette.leftArm = BuildArm(type, model, -halfWidth * 0.68f, shoulderWidth, armLength,
                primary, secondary, joints, armorMesh, emission, false);
            silhouette.rightArm = BuildArm(type, model, halfWidth * 0.68f, shoulderWidth, armLength,
                primary, secondary, joints, armorMesh, emission, true);

            var hipWidth = type == RoboClashRobotType.Titan ? 0.66f : 0.46f;
            ArmorPiece(armorMesh, "Hip Chassis", model, new Vector3(0f, 0.59f * scale, 0f),
                new Vector3(hipWidth, 0.22f, 0.38f), joints);
            var legGap = type == RoboClashRobotType.Titan ? 0.42f : 0.23f;
            var legLength = type == RoboClashRobotType.Titan ? 0.65f : 0.64f;
            silhouette.leftLeg = BuildLeg(model, "Left", -legGap, legLength, primary, secondary, joints, armorMesh);
            silhouette.rightLeg = BuildLeg(model, "Right", legGap, legLength, primary, secondary, joints, armorMesh);

            if (type == RoboClashRobotType.Titan)
            {
                for (var side = -1; side <= 1; side += 2)
                {
                    var isLarge = side < 0;
                    var shoulder = new Vector3(side * halfWidth * 0.78f, 1.45f * scale, 0f);
                    ArmorPiece(armorMesh, isLarge ? "Titan Asymmetric Bulwark" : "Titan Cannon-Side Guard",
                        model, shoulder,
                        new Vector3(shoulderWidth * (isLarge ? 1.9f : 1.35f), isLarge ? 0.62f : 0.4f,
                            isLarge ? 0.64f : 0.48f), primary, Quaternion.Euler(0f, 0f, side * -8f));
                    ArmorPiece(armorMesh, "Titan Shoulder Inset", model,
                        shoulder + new Vector3(0f, 0.08f, 0.29f),
                        new Vector3(shoulderWidth * 1.05f, 0.12f, 0.05f), secondary);
                }
            }
            else if (type == RoboClashRobotType.Spark)
            {
                for (var side = -1; side <= 1; side += 2)
                    Piece(PrimitiveType.Sphere, "Rounded Utility Shoulder", model,
                        new Vector3(side * halfWidth * 0.72f, 1.43f * scale, 0f),
                        new Vector3(0.42f, 0.38f, 0.38f), primary);
                BuildBackpack(model, dimensions, primary, secondary, emission, armorMesh);
            }
            else if (type == RoboClashRobotType.Volt)
                BuildVoltPowerUnit(model, primary, secondary, emission, armorMesh);
            else if (type == RoboClashRobotType.Shadow)
                BuildShadowEmitters(model, dimensions, emission);

            var weaponMount = BuildWeapon(type, aimPivot, dimensions, primary, secondary, joints, emission,
                armorMesh,
                out var muzzleFlash, out var rotatingBarrels);
            silhouette.weaponMount = weaponMount;
            silhouette.muzzle = weaponMount.Find("Muzzle");
            silhouette.muzzleFlash = muzzleFlash;
            silhouette.rotatingBarrels = rotatingBarrels;

            if (type == RoboClashRobotType.Spark)
                silhouette.repairDrone = BuildRepairDrone(model, primary, secondary, emission);
            if (type == RoboClashRobotType.Shadow)
                silhouette.cloakVisual = BuildCloakShell(model, dimensions, shimmer);

            return silhouette;
        }

        private static Transform BuildArm(RoboClashRobotType type, Transform model, float x,
            float armorWidth, float armLength, Material primary, Material secondary, Material joints,
            Mesh armorMesh, Material emission, bool weaponArm)
        {
            var shoulder = Child(x < 0f ? "Left Shoulder Joint" : "Right Shoulder Joint",
                model, new Vector3(x, 1.39f, 0f));
            Piece(PrimitiveType.Sphere, "Shoulder Actuator", shoulder, Vector3.zero,
                new Vector3(0.28f, 0.3f, 0.3f), joints);
            ArmorPiece(armorMesh, type == RoboClashRobotType.Titan ? "Heavy Shoulder Armor" : "Shoulder Armor",
                shoulder, new Vector3(0f, 0.07f, 0f),
                new Vector3(armorWidth, 0.32f, 0.38f), primary);
            Piece(PrimitiveType.Cylinder, "Upper Arm", shoulder,
                new Vector3(0f, -armLength * 0.7f, 0f),
                new Vector3(0.13f, armLength * 0.55f, 0.13f), secondary);
            Piece(PrimitiveType.Sphere, "Elbow Joint", shoulder,
                new Vector3(0f, -armLength * 1.35f, 0f),
                Vector3.one * 0.17f, joints);
            ArmorPiece(armorMesh, "Forearm Armor", shoulder,
                new Vector3(0f, -armLength * 1.85f, 0.03f),
                new Vector3(0.23f, armLength * 0.75f, 0.28f), primary);
            Piece(PrimitiveType.Cylinder, "Wrist Actuator", shoulder,
                new Vector3(0f, -armLength * 2.22f, 0.04f),
                new Vector3(0.095f, 0.12f, 0.095f), joints);
            ArmorPiece(armorMesh, weaponArm ? "Weapon Grip Hand" : "Support Hand", shoulder,
                new Vector3(0f, -armLength * 2.43f, 0.13f),
                new Vector3(0.2f, 0.2f, 0.22f), joints);
            for (var finger = -1; finger <= 1; finger++)
                Piece(PrimitiveType.Cylinder, "Finger Actuator " + finger, shoulder,
                    new Vector3(finger * 0.055f, -armLength * 2.55f, 0.2f),
                    new Vector3(0.035f, 0.1f, 0.035f), secondary, Quaternion.Euler(90f, 0f, 0f));

            Rod("Forearm Hydraulic", shoulder,
                new Vector3(0.105f, -armLength * 1.3f, -0.06f),
                new Vector3(0.105f, -armLength * 2.1f, -0.06f), 0.045f, emission);
            for (var vent = 0; vent < 3; vent++)
                ArmorPiece(armorMesh, "Forearm Vent " + vent, shoulder,
                    new Vector3(0f, -armLength * (1.6f + vent * 0.13f), 0.18f),
                    new Vector3(0.12f, 0.018f, 0.018f), joints);
            return shoulder;
        }

        private static Transform BuildLeg(Transform model, string side, float x, float length,
            Material primary, Material secondary, Material joints, Mesh armorMesh)
        {
            var hip = Child(side + " Leg", model, new Vector3(x, length * 1.74f, 0f));
            Piece(PrimitiveType.Sphere, "Hip Joint", hip, Vector3.zero, Vector3.one * 0.2f, joints);
            ArmorPiece(armorMesh, "Thigh Armor Shell", hip, new Vector3(0f, -length * 0.39f, 0f),
                new Vector3(0.3f, length * 0.7f, 0.34f), primary);
            ArmorPiece(armorMesh, "Thigh Front Plate", hip, new Vector3(0f, -length * 0.39f, 0.17f),
                new Vector3(0.19f, length * 0.47f, 0.045f), secondary);
            Piece(PrimitiveType.Sphere, "Knee Joint", hip, new Vector3(0f, -length * 0.8f, 0f),
                Vector3.one * 0.16f, joints);
            ArmorPiece(armorMesh, "Knee Guard", hip, new Vector3(0f, -length * 0.8f, 0.14f),
                new Vector3(0.3f, 0.19f, 0.12f), primary);
            ArmorPiece(armorMesh, "Shin Armor", hip, new Vector3(0f, -length * 1.26f, 0f),
                new Vector3(0.25f, length * 0.75f, 0.3f), secondary);
            Piece(PrimitiveType.Cylinder, "Ankle Joint", hip,
                new Vector3(0f, -length * 1.57f, 0f), new Vector3(0.1f, 0.11f, 0.1f), joints);
            ArmorPiece(armorMesh, "Articulated Foot", hip, new Vector3(0f, -length * 1.72f, 0.12f),
                new Vector3(0.39f, 0.14f, 0.49f), joints);
            ArmorPiece(armorMesh, "Toe Armor", hip, new Vector3(0f, -length * 1.72f, 0.31f),
                new Vector3(0.35f, 0.07f, 0.13f), primary);
            Rod("Knee Hydraulic", hip, new Vector3(0.12f, -length * 0.88f, -0.08f),
                new Vector3(0.12f, -length * 1.46f, -0.08f), 0.04f, joints);
            return hip;
        }

        private static void BuildBackpack(Transform model, Vector3 dimensions,
            Material primary, Material secondary, Material emission, Mesh armorMesh)
        {
            var back = -dimensions.z * 0.5f;
            ArmorPiece(armorMesh, "Support Backpack Housing", model, new Vector3(0f, 1.05f, back),
                new Vector3(0.48f, 0.56f, 0.28f), secondary);
            Piece(PrimitiveType.Cylinder, "Repair Canister Left", model, new Vector3(-0.2f, 1.05f, back - 0.12f),
                new Vector3(0.13f, 0.36f, 0.13f), primary);
            Piece(PrimitiveType.Cylinder, "Repair Canister Right", model, new Vector3(0.2f, 1.05f, back - 0.12f),
                new Vector3(0.13f, 0.36f, 0.13f), primary);
            ArmorPiece(armorMesh, "Repair Module Hatch", model, new Vector3(0f, 1.05f, back - 0.16f),
                new Vector3(0.25f, 0.32f, 0.05f), primary);
            Piece(PrimitiveType.Sphere, "Backpack Status Light", model,
                new Vector3(0f, 1.26f, back - 0.17f), Vector3.one * 0.12f, emission);
            Rod("Left Backpack Cable", model, new Vector3(-0.22f, 0.83f, back + 0.08f),
                new Vector3(-0.16f, 0.55f, 0.06f), 0.035f, emission);
            Rod("Right Backpack Cable", model, new Vector3(0.22f, 0.83f, back + 0.08f),
                new Vector3(0.16f, 0.55f, 0.06f), 0.035f, emission);
        }

        private static Transform BuildWeapon(RoboClashRobotType type, Transform aimPivot, Vector3 dimensions,
            Material primary, Material secondary, Material joints, Material emission, Mesh armorMesh,
            out Renderer muzzleFlash, out Transform rotatingBarrels)
        {
            var mount = Child("WeaponMount", aimPivot,
                new Vector3(type == RoboClashRobotType.Spark ? 0.32f : 0.22f, -0.5f, dimensions.z * 0.2f));
            var gun = Child(type == RoboClashRobotType.Volt ? "Pulse Rifle" :
                type == RoboClashRobotType.Titan ? "Gatling Cannon" :
                type == RoboClashRobotType.Shadow ? "Energy SMG" : "Shoulder Rocket Launcher", mount, Vector3.zero);

            if (type == RoboClashRobotType.Titan)
            {
                ArmorPiece(armorMesh, "Cannon Housing", gun, new Vector3(0f, 0.02f, 0.05f),
                    new Vector3(0.52f, 0.4f, 0.62f), primary);
                ArmorPiece(armorMesh, "Cannon Mounting Bracket", gun, new Vector3(-0.2f, -0.25f, -0.08f),
                    new Vector3(0.28f, 0.18f, 0.4f), joints);
                rotatingBarrels = Child("Rotating Gatling Barrels", gun, new Vector3(0f, 0f, 0.32f));
                for (var i = 0; i < 4; i++)
                {
                    var angle = i * 90f * Mathf.Deg2Rad;
                    Piece(PrimitiveType.Cylinder, "Cannon Barrel " + (i + 1), rotatingBarrels,
                        new Vector3(Mathf.Cos(angle) * 0.16f, Mathf.Sin(angle) * 0.16f, 0.4f),
                        new Vector3(0.09f, 0.37f, 0.09f), secondary, Quaternion.Euler(90f, 0f, 0f));
                    Piece(PrimitiveType.Cylinder, "Red Barrel Core " + (i + 1), rotatingBarrels,
                        new Vector3(Mathf.Cos(angle) * 0.16f, Mathf.Sin(angle) * 0.16f, 0.78f),
                        new Vector3(0.045f, 0.04f, 0.045f), emission);
                }

                var titanMuzzle = Child("Muzzle", rotatingBarrels, new Vector3(0f, 0f, 0.86f));
                Piece(PrimitiveType.Sphere, "MuzzleFlash", titanMuzzle, Vector3.zero,
                    Vector3.one * 0.24f, emission).gameObject.name = "MuzzleFlash";
                muzzleFlash = titanMuzzle.GetComponentInChildren<Renderer>();
                muzzleFlash.enabled = false;
                for (var side = -1; side <= 1; side += 2)
                    Piece(PrimitiveType.Cylinder, "Rotary Motor", gun, new Vector3(side * 0.27f, -0.15f, 0.12f),
                        new Vector3(0.11f, 0.18f, 0.11f), joints, Quaternion.Euler(0f, 0f, 90f));
                return mount;
            }
            rotatingBarrels = null;

            var spark = type == RoboClashRobotType.Spark;
            var shadow = type == RoboClashRobotType.Shadow;
            var bodyMaterial = spark || shadow ? secondary : primary;
            ArmorPiece(armorMesh, "Weapon Receiver", gun, Vector3.zero,
                spark ? new Vector3(0.4f, 0.42f, 0.48f) : shadow ? new Vector3(0.26f, 0.23f, 0.52f) :
                new Vector3(0.3f, 0.28f, 0.62f), bodyMaterial);
            Piece(PrimitiveType.Cylinder, spark ? "Rocket Tube" : "Weapon Barrel", gun,
                new Vector3(0f, 0f, 0.43f),
                new Vector3(spark ? 0.19f : shadow ? 0.075f : 0.1f, spark ? 0.3f : 0.34f,
                    spark ? 0.19f : shadow ? 0.075f : 0.1f),
                secondary, Quaternion.Euler(90f, 0f, 0f));
            Piece(PrimitiveType.Cylinder, "Muzzle Shroud", gun, new Vector3(0f, 0f, 0.66f),
                new Vector3(spark ? 0.22f : 0.12f, 0.07f, spark ? 0.22f : 0.12f),
                joints, Quaternion.Euler(90f, 0f, 0f));
            ArmorPiece(armorMesh, spark ? "Rocket Magazine" : "Weapon Energy Cell",
                gun, new Vector3(0f, -0.19f, 0.06f),
                spark ? new Vector3(0.22f, 0.29f, 0.24f) : new Vector3(0.17f, 0.23f, 0.19f),
                spark ? primary : emission);
            Piece(PrimitiveType.Sphere, "Weapon Energy Core", gun, new Vector3(0f, 0.16f, 0.08f),
                Vector3.one * 0.13f, emission);
            for (var side = -1; side <= 1; side += 2)
                ArmorPiece(armorMesh, "Weapon Side Rail " + side, gun,
                    new Vector3(side * (spark ? 0.21f : 0.16f), 0f, 0.06f),
                    new Vector3(0.035f, 0.08f, 0.42f), joints);
            var muzzle = Child("Muzzle", gun, new Vector3(0f, 0f, spark ? 0.76f : 0.61f));
            var flash = Piece(PrimitiveType.Sphere, "MuzzleFlash", muzzle, Vector3.zero,
                Vector3.one * (spark ? 0.3f : 0.17f), emission);
            muzzleFlash = flash.GetComponent<Renderer>();
            muzzleFlash.enabled = false;
            return mount;
        }

        private static Transform BuildRepairDrone(Transform model, Material primary, Material secondary, Material emission)
        {
            var drone = Child("Repair Drone", model, new Vector3(0.9f, 1.72f, -0.35f));
            Piece(PrimitiveType.Sphere, "Drone Core", drone, Vector3.zero,
                new Vector3(0.4f, 0.25f, 0.34f), secondary);
            Piece(PrimitiveType.Cube, "Drone Yellow Shell", drone, Vector3.zero,
                new Vector3(0.3f, 0.15f, 0.28f), primary);
            Piece(PrimitiveType.Sphere, "Drone Status Light", drone,
                new Vector3(0f, 0f, 0.18f), Vector3.one * 0.1f, emission);
            for (var side = -1; side <= 1; side += 2)
                Piece(PrimitiveType.Cylinder, "Hover Rotor", drone, new Vector3(side * 0.25f, 0f, 0f),
                    new Vector3(0.07f, 0.16f, 0.07f), emission);
            return drone;
        }

        private static void BuildVoltPowerUnit(Transform model, Material primary, Material secondary,
            Material emission, Mesh armorMesh)
        {
            ArmorPiece(armorMesh, "Volt Back Power Unit", model, new Vector3(0f, 1.05f, -0.34f),
                new Vector3(0.42f, 0.48f, 0.28f), secondary);
            for (var side = -1; side <= 1; side += 2)
            {
                Piece(PrimitiveType.Cylinder, "Power Cell " + side, model,
                    new Vector3(side * 0.22f, 1.08f, -0.42f),
                    new Vector3(0.1f, 0.32f, 0.1f), primary);
                Piece(PrimitiveType.Sphere, "Power Cell Indicator " + side, model,
                    new Vector3(side * 0.22f, 1.08f, -0.5f),
                    Vector3.one * 0.07f, emission);
            }
            Rod("Power Conduit Left", model, new Vector3(-0.18f, 0.86f, -0.25f),
                new Vector3(-0.14f, 0.67f, 0.12f), 0.035f, emission);
            Rod("Power Conduit Right", model, new Vector3(0.18f, 0.86f, -0.25f),
                new Vector3(0.14f, 0.67f, 0.12f), 0.035f, emission);
        }

        private static void BuildShadowEmitters(Transform model, Vector3 dimensions, Material emission)
        {
            for (var side = -1; side <= 1; side += 2)
            {
                Piece(PrimitiveType.Sphere, "Stealth Emitter " + side, model,
                    new Vector3(side * dimensions.x * 0.42f, 1.2f, -dimensions.z * 0.45f),
                    new Vector3(0.1f, 0.16f, 0.1f), emission);
                Rod("Shadow Energy Conduit " + side, model,
                    new Vector3(side * 0.18f, 1.22f, 0.24f),
                    new Vector3(side * 0.17f, 0.72f, 0.18f), 0.024f, emission);
            }
        }

        private static Renderer BuildCloakShell(Transform model, Vector3 dimensions, Material shimmer)
        {
            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = "CloakVisual";
            shell.transform.SetParent(model, false);
            shell.transform.localPosition = Vector3.up * (dimensions.y * 0.5f);
            shell.transform.localScale = new Vector3(dimensions.x * 1.08f, dimensions.y * 0.58f, dimensions.z * 1.15f);
            DestroyCollider(shell.GetComponent<Collider>());
            var renderer = shell.GetComponent<Renderer>();
            if (shimmer != null)
                renderer.sharedMaterial = shimmer;
            renderer.enabled = false;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        public static Mesh CreateArmorMesh()
        {
            const int sides = 8;
            var vertices = new Vector3[sides * 4 + 2];
            var cuts = new[]
            {
                new Vector2(-0.5f + 0.14f, -0.5f), new Vector2(0.5f - 0.14f, -0.5f),
                new Vector2(0.5f, -0.5f + 0.14f), new Vector2(0.5f, 0.5f - 0.14f),
                new Vector2(0.5f - 0.14f, 0.5f), new Vector2(-0.5f + 0.14f, 0.5f),
                new Vector2(-0.5f, 0.5f - 0.14f), new Vector2(-0.5f, -0.5f + 0.14f)
            };
            var rings = new[] { (z: -0.5f, scale: 0.82f), (z: -0.42f, scale: 1f),
                (z: 0.42f, scale: 1f), (z: 0.5f, scale: 0.82f) };
            for (var ring = 0; ring < rings.Length; ring++)
            {
                for (var i = 0; i < sides; i++)
                {
                    var point = cuts[i] * rings[ring].scale;
                    vertices[ring * sides + i] = new Vector3(point.x, point.y, rings[ring].z);
                }
            }

            vertices[vertices.Length - 2] = new Vector3(0f, 0f, -0.5f);
            vertices[vertices.Length - 1] = new Vector3(0f, 0f, 0.5f);
            var triangles = new int[sides * ((rings.Length - 1) * 6 + 6)];
            var index = 0;
            for (var ring = 0; ring < rings.Length - 1; ring++)
            {
                for (var i = 0; i < sides; i++)
                {
                    var a = ring * sides + i;
                    var b = ring * sides + (i + 1) % sides;
                    var c = (ring + 1) * sides + (i + 1) % sides;
                    var d = (ring + 1) * sides + i;
                    triangles[index++] = a;
                    triangles[index++] = b;
                    triangles[index++] = c;
                    triangles[index++] = a;
                    triangles[index++] = c;
                    triangles[index++] = d;
                }
            }

            var backCenter = vertices.Length - 2;
            var frontCenter = vertices.Length - 1;
            for (var i = 0; i < sides; i++)
            {
                triangles[index++] = backCenter;
                triangles[index++] = (i + 1) % sides;
                triangles[index++] = i;
                var start = (rings.Length - 1) * sides;
                triangles[index++] = frontCenter;
                triangles[index++] = start + i;
                triangles[index++] = start + (i + 1) % sides;
            }

            var mesh = new Mesh { name = "RoboClash Beveled Armor" };
            mesh.vertices = vertices;
            var uvs = new Vector2[vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
                uvs[i] = new Vector2(vertices[i].x + 0.5f, vertices[i].y + 0.5f);
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Transform Child(string name, Transform parent, Vector3 localPosition)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }

        private static Renderer ArmorPiece(Mesh mesh, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material, Quaternion? rotation = null)
        {
            if (mesh == null)
                return Piece(PrimitiveType.Cube, name, parent, localPosition, localScale, material, rotation);

            var piece = new GameObject(name);
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
            if (rotation.HasValue)
                piece.transform.localRotation = rotation.Value;
            piece.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = piece.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static void Rod(string name, Transform parent, Vector3 start, Vector3 end,
            float radius, Material material)
        {
            var direction = end - start;
            var rod = Piece(PrimitiveType.Cylinder, name, parent, (start + end) * 0.5f,
                new Vector3(radius, direction.magnitude * 0.5f, radius), material);
            rod.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        }

        private static Transform FindDescendant(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;
                var descendant = FindDescendant(child, childName);
                if (descendant != null)
                    return descendant;
            }

            return null;
        }

        private static Renderer Piece(PrimitiveType primitive, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material, Quaternion? rotation = null)
        {
            var piece = GameObject.CreatePrimitive(primitive);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
            if (rotation.HasValue)
                piece.transform.localRotation = rotation.Value;
            var collider = piece.GetComponent<Collider>();
            DestroyCollider(collider);
            var renderer = piece.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static void DestroyCollider(Collider collider)
        {
            if (collider == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(collider);
            else
                Object.DestroyImmediate(collider);
        }

        private sealed class RobotSilhouette
        {
            public Transform aimPivot;
            public Transform weaponMount;
            public Transform muzzle;
            public Transform rotatingBarrels;
            public Transform repairDrone;
            public Transform leftLeg;
            public Transform rightLeg;
            public Transform leftArm;
            public Transform rightArm;
            public Transform torso;
            public Renderer muzzleFlash;
            public Renderer cloakVisual;
        }
    }
}
