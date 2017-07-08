<?xml version="1.0" encoding="UTF-8"?>

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
xmlns:msxsl="urn:schemas-microsoft-com:xslt">

   <xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>

   <!--xsl:template match="@*|node()">
      <xsl:copy>
         <xsl:apply-templates select="@*|node()"/>
      </xsl:copy>
   </xsl:template-->

	<xsl:template match="MovieInfo">
		<xsl:element name="movie">
			<xsl:element name="title"><xsl:value-of select="Title"/></xsl:element>
			<xsl:element name="originaltitle"><xsl:value-of select="OriginalTitle"/></xsl:element>
			<xsl:element name="plot"><xsl:value-of select="Plot"/></xsl:element>
			<xsl:element name="year"><xsl:value-of select="Year"/></xsl:element>
			<xsl:element name="runtime"><xsl:value-of select="Runtime"/></xsl:element>
			<xsl:element name="rating"><xsl:value-of select="Rating"/></xsl:element>
			<xsl:element name="certification"><xsl:value-of select="Certification"/></xsl:element>
			
			<xsl:apply-templates select="Genres"/>
			<xsl:apply-templates select="Actors"/>
			<xsl:apply-templates select="Directors"/>
			<xsl:apply-templates select="Countries"/>
		</xsl:element>
	</xsl:template>

	<xsl:template match="Genres">
		<xsl:element name="genre">
			<xsl:for-each select="string">
				<xsl:element name="name"><xsl:value-of select="."></xsl:value-of></xsl:element>
			</xsl:for-each>
		</xsl:element>
	</xsl:template>

	<xsl:template match="Actors">
		<xsl:element name="actor">
			<xsl:for-each select="string">
				<xsl:element name="name"><xsl:value-of select="."></xsl:value-of></xsl:element>
			</xsl:for-each>
		</xsl:element>
	</xsl:template>

	<xsl:template match="Directors">
		<xsl:element name="director">
			<xsl:for-each select="string">
				<xsl:element name="name"><xsl:value-of select="."></xsl:value-of></xsl:element>
			</xsl:for-each>
		</xsl:element>
	</xsl:template>

	<xsl:template match="Countries">
		<xsl:element name="country">
			<xsl:for-each select="string">
				<xsl:element name="name"><xsl:value-of select="."></xsl:value-of></xsl:element>
			</xsl:for-each>
		</xsl:element>
	</xsl:template>


</xsl:stylesheet>
