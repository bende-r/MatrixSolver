using static MatrixSolverServer.Program;

namespace MatrixSolverServer
{
    // Интерфейс сервиса
    public interface ISolverService
    {
        double[] SolveSLAUWithStripeMultiplication(double[][] matrix, double[] vector);
        // double[] SolveSLAUWithGauss(double[][] matrix, double[] vector);
        void ValidateMatrixRequest(MatrixRequest request);
    }
}
