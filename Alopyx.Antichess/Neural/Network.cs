using System.Collections.Generic;
using System.Linq;

namespace Alopyx.Antichess.Neural
{
    public class Network
    {
        List<Layer> layers = new List<Layer>();

        public void AddLayer(Layer l)
        {
            layers.Add(l);
        }

        public Layer GetLayer(int index)
        {
            return layers[index];
        }

        public Matrix Query(double[] inputs)
        {
            Matrix m = Matrix.Column(inputs);
            foreach (Layer l in layers)
            {
                m = l.Query(m);
            }
            return m;
        }

        public void Train(double[] inputList, double[] targetList)
        {
            Matrix inputs = Matrix.Column(inputList);
            Matrix targets = Matrix.Column(targetList);

            List<Matrix> layerOutputs = new List<Matrix>();
            List<Matrix> outputErrors = new List<Matrix>();

            Matrix output = Matrix.Column(inputList);
            foreach (Layer l in layers)
            {
                output = l.Query(output);
                layerOutputs.Add(output);
            }

            outputErrors.Add(targets.Add(layerOutputs.Last().ApplyOnAllElements(x => -x)));
            for (int i = 0; i < layers.Count; i++)
            {
                Layer currentLayer = layers[layers.Count - i - 1];
                Matrix currentOutput = layerOutputs[layerOutputs.Count - i - 1];
                Matrix currentError;
                if (i != 0)
                {
                    currentError = layers[layers.Count - i].Weights.Transpose().Multiply(outputErrors[i - 1]);
                    outputErrors.Add(currentError);
                }
                else
                {
                    currentError = outputErrors[0].ApplyOnAllElements(x => x);
                }

                Matrix layerInputs;
                if (layerOutputs.Count - i == 1)
                {
                    layerInputs = inputs;
                }
                else
                {
                    layerInputs = layerOutputs[layerOutputs.Count - i - 2];
                }
                currentLayer.ApplyError(currentError, currentOutput, layerInputs);
            }
        }
    }
}
