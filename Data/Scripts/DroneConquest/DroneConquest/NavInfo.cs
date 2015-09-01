using System;
using VRage.ModAPI;
using VRageMath;

namespace DroneConquest
{
    internal class NavInfo
    {
        private Vector3D _fromPosition;
        private Vector3D _toPosition;
        private IMyEntity _shipControls;
        private Vector3D _dir;
        private Vector2 _rot;
        private float _roll;

        public NavInfo(Vector3D fromPosition, Vector3D toPosition, IMyEntity shipControls)
        {
            _fromPosition = fromPosition;
            _toPosition = toPosition;
            _shipControls = shipControls;
            Init();
        }

        public float Roll
        {
            get { return _roll; }
            set { _roll = value; }
        }

        public Vector2 Rotation
        {
            get { return _rot; }
            set { _rot = value; }
        }

        public Vector3D Direction
        {
            get { return _dir; }
            set { _dir = value; }
        }

        public static Vector2 CalculateRotation(Vector3D direction, IMyEntity _shipControls)
        {
            var _dir = direction - _shipControls.GetPosition();
            var dirNorm = Vector3D.Normalize(_dir);
            var x = -(_shipControls as IMyEntity).WorldMatrix.Up.Dot(dirNorm);
            var y = -(_shipControls as IMyEntity).WorldMatrix.Left.Dot(dirNorm);
            var forw = (_shipControls as IMyEntity).WorldMatrix.Forward.Dot(dirNorm);

            if (forw < 0)
                y = 0;
            if (Math.Abs(x) < 0.07f)
                x = 0;
            if (Math.Abs(y) < 0.07f)
                y = 0;

            return new Vector2((float)x, (float)y);
        }

        public void Init()
        {
            _dir = _toPosition - _fromPosition;

            var dirNorm = Vector3D.Normalize(_dir);
            var x = -(_shipControls as IMyEntity).WorldMatrix.Up.Dot(dirNorm);
            var y = -(_shipControls as IMyEntity).WorldMatrix.Left.Dot(dirNorm);
            var forw = (_shipControls as IMyEntity).WorldMatrix.Forward.Dot(dirNorm);

            if (forw < 0)
                y = 0;
            if (Math.Abs(x) < 0.07f)
                x = 0;
            if (Math.Abs(y) < 0.07f)
                y = 0;


            if (_dir.Length() < 30)
                _dir = Vector3D.Zero;
            else
                _dir = Vector3D.TransformNormal(_dir, (_shipControls as IMyEntity).WorldMatrixNormalizedInv);

            _rot = new Vector2((float) x, (float) y);
            _roll = 0;
        }
    }
}
