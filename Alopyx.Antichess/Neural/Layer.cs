using System;

namespace Alopyx.Antichess.Neural
{
    public class Layer
    {
        public int InputNodes { get; private set; }
        public int OutputNodes { get; private set; }
        public double LearningRate { get; private set; }
        public Matrix Weights { get; private set; }
        public Func<double, double> Activation { get; private set; }

        public Layer(int inputNodes, int outputNodes, double learningRate)
        {
            InputNodes = inputNodes;
            OutputNodes = outputNodes;
            LearningRate = learningRate;
            Matrix m = new Matrix(outputNodes, inputNodes);
            m.InitializeRandomly();
            Weights = m.ApplyOnAllElements(x => x - 0.5);
            Activation = x => 1 / (1 + Math.Pow(Math.E, -x));
        }

        public Layer(int inputNodes, int outputNodes, double learningRate, Matrix initWeights)
        {
            InputNodes = inputNodes;
            OutputNodes = outputNodes;
            LearningRate = learningRate;
            Weights = initWeights;
            Activation = x => 1 / (1 + Math.Pow(Math.E, -x));
        }

        public Matrix Query(Matrix inputs)
        {
            return Weights.Multiply(inputs).ApplyOnAllElements(Activation);
        }

        public void ApplyError(Matrix error, Matrix outputs, Matrix inputs)
        {
            Weights = Weights.Add(error
                .MultiplyOneOnOne(outputs)
                .MultiplyOneOnOne(outputs.ApplyOnAllElements(x => 1.0 - x))
                .Multiply(inputs.Transpose())
                .ApplyOnAllElements(x => x * LearningRate));
        }
    }
}
