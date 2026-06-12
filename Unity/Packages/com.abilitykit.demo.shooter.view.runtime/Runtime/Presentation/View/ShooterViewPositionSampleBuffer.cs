using System.Numerics;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterViewPositionSampleBuffer
    {
        private const int DefaultCapacity = 64;
        
        private ShooterViewPositionSample[] _samples;
        private int _head;
        private int _count;

        public ShooterViewPositionSampleBuffer(int capacity = DefaultCapacity)
        {
            _samples = new ShooterViewPositionSample[capacity];
        }

        public int Count => _count;

        public void Add(float timeTicks, Vector3 position, Quaternion rotation)
        {
            if (_count == _samples.Length)
            {
                var newSamples = new ShooterViewPositionSample[_samples.Length * 2];
                for (int i = 0; i < _count; i++)
                {
                    newSamples[i] = _samples[(_head + i) % _samples.Length];
                }
                _samples = newSamples;
                _head = 0;
            }

            int index = (_head + _count) % _samples.Length;
            _samples[index] = new ShooterViewPositionSample
            {
                TimeTicks = timeTicks,
                Position = position,
                Rotation = rotation
            };
            _count++;
        }

        public bool TryEvaluate(float targetTimeTicks, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.Zero;
            rotation = Quaternion.Identity;

            if (_count < 2)
                return false;

            int tail = (_head + _count - 1) % _samples.Length;
            
            if (targetTimeTicks <= _samples[_head].TimeTicks)
            {
                position = _samples[_head].Position;
                rotation = _samples[_head].Rotation;
                return true;
            }

            if (targetTimeTicks >= _samples[tail].TimeTicks)
            {
                position = _samples[tail].Position;
                rotation = _samples[tail].Rotation;
                return true;
            }

            for (int i = 0; i < _count - 1; i++)
            {
                int current = (_head + i) % _samples.Length;
                int next = (_head + i + 1) % _samples.Length;

                if (_samples[current].TimeTicks <= targetTimeTicks && 
                    _samples[next].TimeTicks >= targetTimeTicks)
                {
                    float t = (targetTimeTicks - _samples[current].TimeTicks) / 
                              (_samples[next].TimeTicks - _samples[current].TimeTicks);
                    
                    position = Vector3.Lerp(_samples[current].Position, _samples[next].Position, t);
                    rotation = Quaternion.Lerp(_samples[current].Rotation, _samples[next].Rotation, t);
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
        }
    }
}