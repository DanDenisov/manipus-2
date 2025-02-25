﻿using System;
using System.Linq;
using OpenTK;

namespace Logic.InverseKinematics
{
    class Jacobian : IKSolver
    {
        public Jacobian(Obstacle[] obstacles, float precision, float stepSize, int maxTime) : 
            base(obstacles, precision, stepSize, maxTime) { }

        public override (bool, float, float[], bool[]) Execute(Manipulator agent, Vector3 goal, int joint)
        {
            /*Vector offset = new Vector(agent.Joints.Length);
            for (int j = 0; j < 10; j++)
            {
                var grip = agent.DKP[joint];
                Vector3 gPos = new Vector3(grip.X, grip.Y, grip.Z);
                Vector3 tPos = new Vector3(goal.X, goal.Y, goal.Z);
                Vector3 error = tPos - gPos;

                // get all joints parameters (positions/axes)
                var joints = agent.Joints;
                var dh = agent.DH;
                //var jointsCount = agent.Joints.Length;

                Matrix4 model = Matrix4.Identity;

                Vector3[] jAxes = new Vector3[joint];
                Vector3[] jPos = new Vector3[joint];

                jAxes[0] = Vector3.UnitY;
                jPos[0] = Vector3.Zero;

                Vector4 initAxis = Vector4.UnitY;
                for (int i = 0; i < joint; i++)  // TODO: all the manipulator structure logic should be incapsulated (elsewhere)
                {
                    model *= Matrix.RotateY(dh[i].theta);
                    model *= Matrix.Translate(dh[i].d * Vector3.UnitY);
                    model *= Matrix.RotateX(dh[i].alpha);
                    model *= Matrix.Translate(dh[i].r * Vector3.UnitX);

                    if (i < joint - 1)
                    {
                        jAxes[i + 1] = (Vector3)(model * initAxis).Normalized();
                        jPos[i + 1] = (Vector3)model.Column3;  // TODO: is it safe?
                    }
                }

                // calculate the Jacobian
                float[,] data = new float[6, joint];
                for (int i = 0; i < joint; i++)
                {
                    var elem = Vector3.Cross(jAxes[i], gPos - jPos[i]);
                    data[0, i] = elem.X;
                    data[1, i] = elem.Y;
                    data[2, i] = elem.Z;
                    data[3, i] = jAxes[i].X;
                    data[4, i] = jAxes[i].Y;
                    data[5, i] = jAxes[i].Z;
                }

                Matrix J = new Matrix(data);

                // get transpose of the Jacobian
                Matrix Jtr = Matrix.Transpose(J);

                // calculate GC offsets
                Vector dq = Jtr * new Vector(new float[6] { error.X, error.Y, error.Z, 0, 0, 0 });
                dq *= -0.1f;
                if (joint < agent.Joints.Length)
                    dq.Expand(agent.Joints.Length - joint);

                // checking for collisions of the found configuration if the algorithm has converged
                agent.q = agent.q.Zip(dq.Components, (t, s) => t + s).ToArray();
                bool[] collisions = new bool[agent.q.Length - 1];
                collisions = DetectCollisions(agent, Obstacles);

                offset += dq;
                var dist = agent.GripperPos.DistanceTo(goal);

                if (j == 9)
                    return (true, dist, offset.Components, collisions);
            }*/

            return default;
        }
    }
}
