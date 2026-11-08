using NUnit.Framework;

namespace RoboClash.Tests
{
    public class PhoneInputParserTests
    {
        private const string OldPacket =
            "{\"type\":\"input\",\"protocol\":1,\"moveForward\":0,\"aimHorizontal\":0,\"gyroTurn\":0,\"gyroPitch\":0,\"gyroYawDelta\":0,\"gyroPitchDelta\":0,\"shoot\":false,\"jump\":false,\"reload\":false,\"ability\":false,\"ultimate\":false}";
        private const string NewPacket =
            "{\"type\":\"input\",\"protocol\":1,\"moveForward\":0,\"aimHorizontal\":0,\"gyroTurn\":0,\"gyroPitch\":0,\"gyroYawDelta\":0,\"gyroPitchDelta\":0,\"shoot\":false,\"jump\":false,\"reload\":false,\"ability\":false,\"ultimate\":false,\"proximityNear\":true,\"compassValid\":true,\"compassHeading\":-45.5}";
        private const string SensorPacket =
            "{\"type\":\"input\",\"protocol\":1,\"gyroYawDelta\":1.5,\"gyroPitchDelta\":-0.5,\"compassValid\":true,\"compassHeading\":90,\"accelerometerValid\":true,\"accelX\":0.1,\"accelY\":0.2,\"accelZ\":9.8,\"recenter\":true}";

        [Test]
        public void OldPacketDefaultsNewFields()
        {
            Assert.IsTrue(RoboClashPhoneInputServer.TryParseInputLineForTest(
                OldPacket, out var parsed));
            Assert.IsFalse(parsed.Guard);
            Assert.IsFalse(parsed.CompassValid);
            Assert.AreEqual(0f, parsed.CompassHeading);
        }

        [Test]
        public void NewPacketParsesGuardAndCompass()
        {
            Assert.IsTrue(RoboClashPhoneInputServer.TryParseInputLineForTest(
                NewPacket, out var parsed));
            Assert.IsTrue(parsed.Guard);
            Assert.IsTrue(parsed.CompassValid);
            Assert.AreEqual(-45.5f, parsed.CompassHeading);
        }

        [Test]
        public void SensorPacketParsesAccelerometerAndRecenter()
        {
            Assert.IsTrue(RoboClashPhoneInputServer.TryParseInputLineForTest(
                SensorPacket, out var parsed));
            Assert.IsTrue(parsed.AccelerometerValid);
            Assert.AreEqual(0.1f, parsed.AccelerometerX);
            Assert.AreEqual(0.2f, parsed.AccelerometerY);
            Assert.AreEqual(9.8f, parsed.AccelerometerZ);
            Assert.IsTrue(parsed.Recenter);
        }

        [Test]
        public void CompassHeadingIsClampedToValidRange()
        {
            const string line =
                "{\"type\":\"input\",\"protocol\":1,\"compassHeading\":999}";
            Assert.IsTrue(RoboClashPhoneInputServer.TryParseInputLineForTest(
                line, out var parsed));
            Assert.AreEqual(180f, parsed.CompassHeading);
        }

        [Test]
        public void NonFiniteCompassHeadingIsRejected()
        {
            const string line =
                "{\"type\":\"input\",\"protocol\":1,\"compassHeading\":NaN}";
            Assert.IsFalse(RoboClashPhoneInputServer.TryParseInputLineForTest(
                line, out _));
        }

        [Test]
        public void UnknownFieldsAreIgnored()
        {
            const string line =
                "{\"type\":\"input\",\"protocol\":1,\"foo\":1}";
            Assert.IsTrue(RoboClashPhoneInputServer.TryParseInputLineForTest(
                line, out var parsed));
            Assert.IsFalse(parsed.Guard);
            Assert.IsFalse(parsed.CompassValid);
            Assert.AreEqual(0f, parsed.CompassHeading);
        }

        [Test]
        public void GarbageLineReturnsFalseWithoutThrowing()
        {
            var result = true;
            ParsedPhoneInput parsed = default;
            Assert.DoesNotThrow(() =>
                result = RoboClashPhoneInputServer.TryParseInputLineForTest(
                    "not json", out parsed));
            Assert.IsFalse(result);
        }
    }
}
