using System;

namespace Dapplo.Windows.Com
{
	/// <summary>
	///     An attribute to specifiy the ProgID of the COM class to create. (As suggested by Kristen Wegner)
	/// </summary>
	[AttributeUsage(AttributeTargets.Interface)]
	public sealed class ComProgIdAttribute : Attribute
	{
		/// <summary>Constructor</summary>
		/// <param name="value">The COM ProgID.</param>
		public ComProgIdAttribute(string value)
		{
			Value = value;
		}

		/// <summary>
		///     Returns the COM ProgID
		/// </summary>
		public string Value { get; }

		/// <summary>
		///     Extracts the attribute from the specified type.
		/// </summary>
		/// <param name="interfaceType">
		///     The interface type.
		/// </param>
		/// <returns>
		///     The <see cref="ComProgIdAttribute" />.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="interfaceType" /> is <see langword="null" />.
		/// </exception>
		public static ComProgIdAttribute GetAttribute(Type interfaceType)
		{
			if (null == interfaceType)
			{
				throw new ArgumentNullException(nameof(interfaceType));
			}

			var attributeType = typeof(ComProgIdAttribute);
			var attributes = interfaceType.GetCustomAttributes(attributeType, false);

			if (0 == attributes.Length)
			{
				var interfaces = interfaceType.GetInterfaces();
				foreach (var t in interfaces)
				{
					interfaceType = t;
					attributes = interfaceType.GetCustomAttributes(attributeType, false);
					if (0 != attributes.Length)
					{
						break;
					}
				}
			}

			if (0 == attributes.Length)
			{
				return null;
			}
			return (ComProgIdAttribute) attributes[0];
		}
	}
}