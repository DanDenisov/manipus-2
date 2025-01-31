﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Graphics;
using Logic.PathPlanning;
using OpenTK;

namespace Logic
{
    public struct TupleDH
    {
        public Joint joint;
        public float theta
        {
            get { return joint.q + thetaOffset; }
        }

        public float thetaOffset;
        public float d;
        public float alpha;
        public float r;

        public TupleDH(float thetaOffset, float d, float alpha, float r)
        {
            joint = null;  // TODO: probably better to use class?

            this.thetaOffset = thetaOffset;
            this.d = d;
            this.alpha = alpha;
            this.r = r;
        }

        public TupleDH Copy(bool deep = false)
        {
            return (TupleDH)MemberwiseClone();
        }
    }

    public struct LinkData
    {
        public Model Model;
        public float Length;
    }

    public struct JointData
    {
        public Model Model;
        public float Length;
        public float q;
        public System.Numerics.Vector2 q_ranges;
        public System.Numerics.Vector4 DH;

        public bool ShowTree;
    }

    public struct ObstData
    {
        public float r;
        public System.Numerics.Vector3 c;
        public int Vector3s_num;

        public bool ShowBounding;
    }

    public struct AlgData
    {
        public int AttrNum;

        public float Precision, StepSize;
        public int MaxTime;

        public int k;
        public float d;
    }

    public enum JointType
    {
        Prismatic,  // Translation
        Revolute,  // Rotation
        Cylindrical,  // Translation & rotation
        Spherical,  // Allows three degrees of rotational freedom about the center of the joint. Also known as a ball-and-socket joint
        Planar  // Allows relative translation on a plane and relative rotation about an axis perpendicular to the plane
    }

    public struct Link
    {
        public Model Model;
        public float Length;

        public Link(Model model, float length)
        {
            Model = model;
            Length = length;
        }

        public Link(LinkData data)
        {
            Model = data.Model;
            Length = data.Length;
        }
    }

    public class Joint
    {
        public Model Model;
        public float Length;

        public float q;
        public float[] qRanges;

        public Joint() { }

        public Joint(JointData data)
        {
            Model = data.Model;
            Length = data.Length;
            q = data.q;
            qRanges = new float[2] { data.q_ranges.X, data.q_ranges.Y };
        }

        public Joint Copy(bool deep = false)
        {
            return (Joint)MemberwiseClone();
        }
    }

    public class Manipulator
    {
        public Vector3 Base;

        public Link[] Links;
        public Joint[] Joints;
        public TupleDH[] DH;
        public List<Matrix4> TransMatrices;  // TODO: use quaternions instead of matrices for D-H transformations
        public float WorkspaceRadius;

        public Vector3 Goal;
        public List<Vector3> Path;
        public List<float[]> Configs;
        public Tree Tree;
        public List<Tree.Node> Buffer = new List<Tree.Node>();
        public List<Attractor> GoodAttractors, BadAttractors;
        public Vector3[] Vector3s;
        public Dictionary<string, bool> States;

        public Manipulator(LinkData[] links, JointData[] joints, TupleDH[] DH)
        {
            Links = new Link[links.Length];
            for (int i = 0; i < links.Length; i++)
            {
                Links[i] = new Link(links[i]);
            }

            Joints = new Joint[joints.Length];
            for (int i = 0; i < joints.Length; i++)
            {
                Joints[i] = new Joint(joints[i]);
            }

            Base = new Vector3(Joints[0].Model.Position.X, Joints[0].Model.Position.Y, Joints[0].Model.Position.Z);

            this.DH = DH;
            for (int i = 0; i < DH.Length; i++)
            {
                DH[i].joint = Joints[i];
            }
            UpdateTransMatrices();

            WorkspaceRadius = Links.Sum((link) => { return link.Length; }) + Joints.Sum((joint) => { return joint.Length; });

            States = new Dictionary<string, bool>
            {
                { "Goal", false },
                { "Attractors", false },
                { "Path", false }
            };
        }

        public Manipulator(Manipulator source)  // TODO: make a m.CreateCopy() method, not a constructor! that's much better
        {
            Links = Misc.CopyArray(source.Links);
            Joints = Array.ConvertAll(source.Joints, x => x.Copy());  //Misc.CopyArray(source.Joints);

            Base = source.Base;  // TODO: review referencing

            DH = Array.ConvertAll(source.DH, x => x.Copy());  //Misc.CopyArray(source.DH);
            for (int i = 0; i < DH.Length; i++)
            {
                DH[i].joint = Joints[i];
            }
            UpdateTransMatrices();

            WorkspaceRadius = source.WorkspaceRadius;

            Goal = source.Goal;  // TODO: review referencing

            States = new Dictionary<string, bool>(source.States);
        }


        public void UpdateTransMatrices()
        {
            TransMatrices = new List<Matrix4>();
            for (int i = 0; i < DH.Length; i++)
            {
                TransMatrices.Add(CreateTransMatrix(DH[i]));
            }
        }

        public static Matrix4 CreateTransMatrix(TupleDH DH)  // TODO: optimize; better to call with joint that contains its own DH table
        {
            float cosT = (float)Math.Cos(DH.theta);
            float sinT = (float)Math.Sin(DH.theta);
            float d = (float)DH.d;
            float cosA = (float)Math.Cos(DH.alpha);
            float sinA = (float)Math.Sin(DH.alpha);
            float r = (float)DH.r;

            return new Matrix4(
                new Vector4(cosT, -sinA * sinT, -sinT * cosA, r * cosT),
                new Vector4(0, cosA, -sinA, d),
                new Vector4(sinT, sinA * cosT, cosA * cosT, r * sinT),
                new Vector4(0, 0, 0, 1)
            );
        }

        public Vector3 GripperPos
        {
            get
            {
                Matrix4 pos = new Matrix4
                (
                    new Vector4(1, 0, 0, Base.X),
                    new Vector4(0, 1, 0, Base.Y),
                    new Vector4(0, 0, 1, Base.Z),
                    new Vector4(0, 0, 0, 1)
                );

                for (int i = 0; i < DH.Length; i++)
                {
                    pos *= TransMatrices[i];
                }

                return new Vector3(pos[0, 3], pos[1, 3], pos[2, 3]);  // TODO: too big error (~2nd order)! optimize
            }
        }

        public Vector3[] DKP
        {
            get
            {
                Vector3[] jointsPos = new Vector3[Joints.Length + 1];
                jointsPos[0] = Base;

                Matrix4 pos = new Matrix4
                (
                    new Vector4(1, 0, 0, Base.X),
                    new Vector4(0, 1, 0, Base.Y),
                    new Vector4(0, 0, 1, Base.Z),
                    new Vector4(0, 0, 0, 1)
                );

                for (int i = 0; i < DH.Length; i++)
                {
                    pos *= TransMatrices[i];
                    jointsPos[i + 1] = new Vector3(pos[0, 3], pos[1, 3], pos[2, 3]);
                }

                return jointsPos;
            }
        }

        public float[] q
        {
            get
            {
                float[] q = new float[Joints.Length];
                for (int i = 0; i < Joints.Length; i++)
                {
                    q[i] = Joints[i].q;
                }
                return q;
            }
            set
            {
                for (int i = 0; i < Joints.Length; i++)
                {
                    Joints[i].q = value[i];
                }

                UpdateTransMatrices();
            }
        }

        public bool InWorkspace(Vector3 point)
        {
            if (point.DistanceTo(Vector3.Zero) - point.DistanceTo(Vector3.Zero) > WorkspaceRadius)
                return false;
            else
                return true;
        }

        public float DistanceTo(Vector3 p)
        {
            return new Vector3(GripperPos, p).Length;
        }

        public void Draw(Shader shader)
        {
            Dispatcher.UpdateConfig.Reset();

            Vector4[] axes = new Vector4[Links.Length];
            Vector4[] pos = new Vector4[Links.Length];

            axes[0] = Vector4.UnitY;
            pos[0] = new Vector4(Vector3.Zero, 1);

            shader.Use();

            //joints[0].q = time;
            //Joints[1].q += 0.016;
            //joints[2].q = time;

            Matrix4 model;
            var quat = DualQuaternion.Default;

            // joints
            for (int i = 0; i < Links.Length; i++)
            {
                quat *= new DualQuaternion(Vector3.UnitY, -(float)DH[i].theta);
                model = quat.Matrix;

                shader.SetMatrix4("model", model, true);
                Joints[i].Model.Position = (Vector3)(model * new Vector4(Joints[0].Model.Position, 1.0f));
                Joints[i].Model.Draw(shader, MeshMode.Solid | MeshMode.Wireframe);

                quat *= new DualQuaternion((float)DH[i].d * Vector3.UnitY);
                quat *= new DualQuaternion(Vector3.UnitX, (float)DH[i].alpha, (float)DH[i].r * Vector3.UnitX);
                model = quat.Matrix;

                if (i < Links.Length - 1)
                {
                    axes[i + 1] = model * axes[0];
                    pos[i + 1] = new Vector4(model.M14, model.M24, model.M34, 1);
                }
            }

            quat = DualQuaternion.Default;

            // links
            quat *= new DualQuaternion(Joints[0].Length / 2 * Vector3.UnitY);

            // the order of multiplication is reversed, because the trans quat transforms the operand (quat) itself; it does not contribute in total transformation like other quats do
            var trans_quat = new DualQuaternion(axes[0].Xyz, pos[0].Xyz, -(float)DH[0].theta);
            quat = trans_quat * quat;
            model = quat.Matrix;

            shader.SetMatrix4("model", model, true);
            Links[0].Model.Draw(shader, MeshMode.Solid | MeshMode.Wireframe);

            quat *= new DualQuaternion((float)DH[0].d * Vector3.UnitY);

            trans_quat = new DualQuaternion(axes[1].Xyz, pos[1].Xyz, -(float)(DH[1].theta + Math.PI / 2));
            quat = trans_quat * quat;
            model = quat.Matrix;

            shader.SetMatrix4("model", model, true);
            Links[1].Model.Draw(shader, MeshMode.Solid | MeshMode.Wireframe);

            quat *= new DualQuaternion((float)DH[1].r * Vector3.UnitY);

            trans_quat = new DualQuaternion(axes[2].Xyz, pos[2].Xyz, -(float)DH[2].theta);
            quat = trans_quat * quat;
            model = quat.Matrix;

            shader.SetMatrix4("model", model, true);
            Links[2].Model.Draw(shader, MeshMode.Solid | MeshMode.Wireframe);

            Dispatcher.UpdateConfig.Set();
        }
    }
}
